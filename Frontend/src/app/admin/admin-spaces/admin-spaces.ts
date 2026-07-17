import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { SpaceBooking } from '../../services/space-booking';
import { WorkSpace } from '../../models/workspace.model';
import { DecimalPipe } from '@angular/common';
import { ToastrService } from 'ngx-toastr';
import { WorkSpaceService } from '../../services/workspace.service';
import { FormBuilder, FormControl, Validators,ReactiveFormsModule, FormGroup, FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';


@Component({
  selector: 'app-admin-spaces',
  imports: [DecimalPipe,ReactiveFormsModule,FormsModule],
  templateUrl: './admin-spaces.html',
  styleUrl: './admin-spaces.scss',
})

export class AdminSpaces implements OnInit {

  private spaceService=inject(WorkSpaceService);
  toastr=inject(ToastrService);
  private fb=inject(FormBuilder);

  spaces=signal<WorkSpace[]>([]);
  searchTerm = signal<string>("");
  currentSpaceId: number | null = null;
  isEditing:boolean=true;
  isSaving = signal<boolean>(false);
  
 


  ngOnInit(): void {
    this.getTotalWorkSpace();
  }


  getTotalWorkSpace(){
   return this.spaceService.getTotalWorkSpace().subscribe({

      next:(data)=>{
        this.spaces.set(data);
      },
      error: (err) => {
        console.error('Error during getting the book spaces: ', err);
      }
    })



  }

  totalSpaces=computed(() => this.spaces().length);
  availableSpaces = computed(() => this.spaces().filter(s => s.isAvailable).length);

  totalCapacity = computed(() => {
  const spaces = this.spaces();
  if (!spaces || spaces.length === 0) return 0;
  return spaces.reduce((acc, s) => acc + (Number(s.capacity) || 0), 0);
  });

  totalRevenue = computed(() => {
  const spaces = this.spaces();
  if (!spaces || spaces.length === 0) return 0;
  
  return spaces.reduce((acc, s) => acc + (Number(s.pricePerHour) || 0), 0);
  });


  filteredSpace = computed(() => {
  const term = this.searchTerm().toLowerCase();
  return this.spaces().filter(s => 
    s.title?.toLowerCase().includes(term)
  );
  });


  onSearchChange(event:Event){
  const value=(event.target as HTMLInputElement).value;
  this.searchTerm.set(value);
  }

  spaceForm=this.fb.group({

   title: ['', [Validators.required, Validators.minLength(5)]],
    description: ['', [Validators.required, Validators.minLength(5)]],
    spaceType: ['', [Validators.required, Validators.minLength(8)]],
    pricePerHour: [0, [Validators.required, Validators.min(0)]],
    pricePerDay: [0, [Validators.required, Validators.min(0)]],
    capacity: [1, [Validators.required, Validators.min(1)]],
    isAvailable:[true,[Validators.required]]
  });


  onSaveSpace() {
  if (!this.spaceForm.valid) {
    this.toastr.error('Please fill in all required fields correctly.');
    this.spaceForm.markAllAsTouched();
    return;
  }

  this.isSaving.set(true);
  const formData = this.spaceForm.value;

  if (this.isEditing) {
    this.spaceService.updateSpace(this.currentSpaceId, formData).subscribe({
      next: () => {
        this.toastr.success('Space updated successfully!');
        this.closeModal();
        this.isSaving.set(false);
        this.getTotalWorkSpace();
      },
      error: (err) => {
        this.toastr.error('Error updating space');
        this.isSaving.set(false); 
      }
    });
  } else {
    this.spaceService.createSpace(formData).subscribe({
      next: () => {
        this.toastr.success('Space created successfully!');
        this.closeModal();
        this.isSaving.set(false); 
        this.getTotalWorkSpace();
      },
      error: (err) => {
        this.toastr.error('Error creating space');
        this.isSaving.set(false); 
      }
    });
  }
}

    closeModal(){
      const btn=document.getElementById('closeModalButton');
      btn?.click();
    }

    onEditSpace(space:WorkSpace){

      this.isEditing=true;
      this.currentSpaceId=space.id;
      this.spaceForm.patchValue(space);
    }

     onAddSpace(){
      this.isEditing=false;
      this.currentSpaceId=null;
      this.spaceForm.reset({
    title: '',
    description: '',
    spaceType: '',
    pricePerHour: 0,
    pricePerDay: 0,
    capacity: 1,
    isAvailable: true
  });
    }

 
}
