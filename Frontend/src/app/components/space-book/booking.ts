import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { WorkSpaceService } from '../../services/workspace.service';
import { SpaceBooking } from '../../services/space-booking';
import { Auth } from '../../services/auth';
import { WorkSpace } from '../../models/workspace.model';

type LoadState = 'loading' | 'ready' | 'not-found' | 'error';

@Component({
  selector: 'app-booking',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './booking.html',
  styleUrl: './booking.scss',
})
export class Booking implements OnInit {

  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  workSpaceService = inject(WorkSpaceService);
  spaceBookingService = inject(SpaceBooking);
  authService = inject(Auth);
  private toastr = inject(ToastrService);

  public currentUser = this.authService.currentUser()?.userInfo.id;

  spaceId = signal<number>(Number(this.route.snapshot.paramMap.get('id')));

  // Workspace details and how loading them went (drives which panel the template shows).
  bookSpace = signal<WorkSpace | null>(null);
  loadState = signal<LoadState>('loading');

  allSlots = signal<string[]>(['08:00:00 - 09:00:00',
    '09:00:00 - 10:00:00', '10:00:00 - 11:00:00', '11:00:00 - 12:00:00',
    '12:00:00 - 13:00:00', '13:00:00 - 14:00:00', '14:00:00 - 15:00:00',
    '15:00:00 - 16:00:00', '16:00:00 - 17:00:00', '17:00:00 - 18:00:00',
    '18:00:00 - 19:00:00', '19:00:00 - 20:00:00'
  ]);

  bookedSlots = signal<string[]>([]);
  // While true (or after a failure) we can't tell which slots are taken, so selection is blocked.
  slotsLoading = signal(false);
  slotsError = signal(false);

  // Local date (not UTC) so late-evening users don't get yesterday/tomorrow.
  readonly today = this.toLocalIsoDate(new Date());
  selectedDate = signal<string>(this.today);

  selectedSlots = signal<string[]>([]);
  submitting = signal(false);

  isUnavailable = computed(() => this.bookSpace()?.isAvailable === false);
  canSelectSlots = computed(() => !this.slotsLoading() && !this.slotsError() && !this.isUnavailable());
  // True when availability is known and every slot on the chosen date is booked or already over.
  noFreeSlots = computed(() => this.canSelectSlots()
    && this.allSlots().every(slot => this.isSlotBooked(slot) || this.isSlotInPast(slot)));

  ngOnInit() {
    this.loadSpace();
  }

  loadSpace() {
    const id = this.spaceId();
    if (!Number.isInteger(id) || id <= 0) {
      this.loadState.set('not-found');
      return;
    }

    this.loadState.set('loading');
    this.workSpaceService.getWorkSpaceById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (space) => {
          this.bookSpace.set(space);
          this.loadState.set('ready');
          this.loadReservedSlots();
        },
        error: (err: HttpErrorResponse) => {
          this.loadState.set(err.status === 404 || err.status === 400 ? 'not-found' : 'error');
        }
      });
  }

  loadReservedSlots() {
    this.slotsLoading.set(true);
    this.slotsError.set(false);
    this.spaceBookingService.getBookedSlots(this.spaceId(), this.selectedDate())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (slots) => {
          this.bookedSlots.set(slots ?? []);
          this.slotsLoading.set(false);
        },
        error: () => {
          this.bookedSlots.set([]);
          this.slotsLoading.set(false);
          this.slotsError.set(true);
        }
      });
  }

  onDateChange(newDate: string) {
    if (!newDate) return;
    this.selectedDate.set(newDate);
    this.selectedSlots.set([]);
    this.loadReservedSlots();
  }

  isSlotBooked(slot: string): boolean {
    const [slotStart, slotEnd] = slot.split(' - ');
    return this.bookedSlots().some(booked => {
      const [rawStart, rawEnd] = booked.split(' - ');
      if (!rawStart || !rawEnd) return false;
      const bookedStart = rawStart.substring(0, 8);
      const bookedEnd = rawEnd.substring(0, 8);
      return slotStart >= bookedStart && slotEnd <= bookedEnd;
    });
  }

  isSlotInPast(slot: string): boolean {
    const [slotStart] = slot.split(' - ');
    const slotDateTime = new Date(`${this.selectedDate()}T${slotStart}`);
    const now = new Date();
    return slotDateTime < now;
  }

  isSlotDisabled(slot: string): boolean {
    return !this.canSelectSlots() || this.isSlotBooked(slot) || this.isSlotInPast(slot);
  }

  selectSlot(slot: string) {
    if (this.isSlotDisabled(slot)) {
      return;
    }

    const currentSelection = this.selectedSlots();
    const index = currentSelection.indexOf(slot);

    if (index > -1) {
      this.selectedSlots.set(currentSelection.filter(s => s !== slot));
    } else {
      this.selectedSlots.set([...currentSelection, slot]);
    }
  }

  // "08:00:00 - 09:00:00" -> "08:00 – 09:00" for display only; the full value is still sent to the API.
  formatSlot(slot: string): string {
    const [start, end] = slot.split(' - ');
    return `${start.substring(0, 5)} – ${end.substring(0, 5)}`;
  }

  isSlotSelected(slot: string): boolean {
    return this.selectedSlots().includes(slot);
  }

  confirmBooking() {
    if (this.submitting()) return;

    if (this.selectedSlots().length === 0 || !this.bookSpace()) {
      this.toastr.error('Please select at least one time slot first!');
      return;
    }

    if (!this.canSelectSlots()) {
      this.toastr.error('Availability could not be confirmed. Please try again.');
      return;
    }

    // 1. ترتيب الأوقات المختارة تصاعدياً لضمان دمجها بشكل متتالٍ وصحيح
    const sortedSlots = [...this.selectedSlots()].sort();

    // 2. فك الساعات لأخذ أول وقت بدأت فيه وأخر وقت انتهت عنده
    const [firstStartTime] = sortedSlots[0].split(' - ');
    const [, lastEndTime] = sortedSlots[sortedSlots.length - 1].split(' - ');

    // 3. بناء الـ Payload الذكي بإرسال المدى الزمني الكلي (مثال: من 09:00:00 إلى 12:00:00)
    const bookingPayload = {
      userId: this.currentUser,
      spaceId: this.bookSpace()?.id,
      bookingDate: this.selectedDate(),
      startTime: firstStartTime.trim(), // البداية الكلية لجميع الساعات المختارة
      endTime: lastEndTime.trim()       // النهاية الكلية لجميع الساعات المختارة
    };

    this.submitting.set(true);
    this.toastr.info(`Processing payment for ${sortedSlots.length} hours...`);

    // 4. إرسال طلب فردي واحد فقط للباك آند
    this.spaceBookingService.creatBooking(bookingPayload).subscribe({
      next: (response) => {
        if (response && response.sessionUrl) {
          this.toastr.success(`Booking created for ${sortedSlots.length} hours. Redirecting to payment...`);
          // التوجيه الفوري لبوابة Stripe بفاتورة شاملة لكل الساعات المدمجة!
          // submitting stays true so the button can't be pressed again during the redirect.
          window.location.href = response.sessionUrl;
        } else {
          this.submitting.set(false);
          this.loadReservedSlots();
          this.toastr.error('Booking saved, but the payment page could not be opened. You can pay it from "My Bookings".');
        }
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.loadReservedSlots();
        this.toastr.error(this.bookingErrorMessage(err));
      }
    });
  }

  private bookingErrorMessage(err: HttpErrorResponse): string {
    switch (err.status) {
      case 0:
        return 'Cannot reach the server. Check your connection and try again.';
      case 401:
        return 'Your session has expired. Please log in again.';
      case 403:
        return 'You are not allowed to make this booking.';
      case 404:
        return 'This workspace no longer exists.';
      default: {
        // The API returns { message } for handled errors, or a plain string for validation errors.
        const serverMessage = typeof err.error === 'string' ? err.error : err.error?.message;
        return serverMessage && err.status < 500 ? serverMessage : 'Failed to save booking. Please try again.';
      }
    }
  }

  private toLocalIsoDate(d: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }
}
