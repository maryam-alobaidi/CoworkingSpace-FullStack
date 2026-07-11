import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { WorkSpaceService } from '../../services/workspace.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms'; 
import { SpaceBooking } from '../../services/space-booking';
import { Auth } from '../../services/auth';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-booking',
  imports: [ CommonModule, FormsModule],
  templateUrl: './booking.html',
  styleUrl: './booking.scss',
})
export class Booking implements OnInit {
  
  private route = inject(ActivatedRoute);
  workSpaceService = inject(WorkSpaceService);
  spaceBookingService = inject(SpaceBooking);
  authService = inject(Auth);
  private toastr = inject(ToastrService);

  public currentUser = this.authService.currentUser()?.userInfo.id ;

  spaceId = signal<number>(parseInt(this.route.snapshot.paramMap.get('id') || '0', 10));
  public bookSpace = toSignal(this.workSpaceService.getWorkSpaceById(this.spaceId()));

  allSlots = signal<string[]>(['08:00:00 - 09:00:00', 
    '09:00:00 - 10:00:00', '10:00:00 - 11:00:00', '11:00:00 - 12:00:00',
    '12:00:00 - 13:00:00', '13:00:00 - 14:00:00', '14:00:00 - 15:00:00',
    '15:00:00 - 16:00:00', '16:00:00 - 17:00:00', '17:00:00 - 18:00:00', 
    '18:00:00 - 19:00:00', '19:00:00 - 20:00:00'
  ]);

  bookedSlots = signal<string[]>([]);
  selectedDate = signal<string>(new Date().toISOString().split('T')[0]);
  

  selectedSlots = signal<string[]>([]);          

  ngOnInit() {
    this.loadReservedSlots(); 
  }

  loadReservedSlots() {
    this.spaceBookingService.getBookedSlots(this.spaceId(), this.selectedDate()).subscribe({
      next: (slots) => {
        this.bookedSlots.set(slots);
      },
      error: (err) => console.error('Error loading slots:', err)
    });
  }
  
  onDateChange(newDate: string) {
    this.selectedDate.set(newDate);
    this.selectedSlots.set([]); 
    this.loadReservedSlots();   
  }

  isSlotBooked(slot: string): boolean {
    const [slotStart, slotEnd] = slot.split(' - ');
    return this.bookedSlots().some(booked => {
      const [rawStart, rawEnd] = booked.split(' - ');
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

  selectSlot(slot: string) {
    if (this.isSlotBooked(slot) || this.isSlotInPast(slot)) {
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

  isSlotSelected(slot: string): boolean {
    return this.selectedSlots().includes(slot);
  }

  confirmBooking() {
    if (this.selectedSlots().length === 0 || !this.bookSpace()) {
      this.toastr.error('Please select at least one time slot first!');
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

    this.toastr.info(`Processing payment for ${sortedSlots.length} hours...`);

    // 4. إرسال طلب فردي واحد فقط للباك آند الحالي لديكِ دون تعديله!
    this.spaceBookingService.creatBooking(bookingPayload).subscribe({
      next: (response) => {
        this.loadReservedSlots();
        this.toastr.success(`Booking created successfully for ${sortedSlots.length} hours!`);
        
        if (response && response.sessionUrl) {
          // التوجيه الفوري لبوابة Stripe بفاتورة شاملة لكل الساعات المدمجة!
          window.location.href = response.sessionUrl;
        } else {
          this.toastr.error('Booking saved, but Stripe Session URL was not found.');
        }
      },
      error: (err) => {
        console.error('Error saving booking:', err);
        this.toastr.error('Failed to save booking. Please try again.');
      }
    });
  }
}