import { Component, inject, OnInit } from '@angular/core'; // 🌟 أضفنا OnInit هنا
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../services/auth';
import { FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-edit-profile',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './edit-profile.html',
  styleUrl: './edit-profile.scss',
})
export class EditProfile implements OnInit { 
  public authService = inject(Auth);
  private toastr = inject(ToastrService);
  private router = inject(Router);

  profileForm = new FormGroup({
    fullName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(3)] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required] }) 
  });
  
  ngOnInit(): void {
    this.fillProfileData();
  }

 
  fillProfileData() {
    const currentUser = this.authService.currentUser();
    if (currentUser && currentUser.userInfo) {
      this.profileForm.patchValue({
        fullName: currentUser.userInfo.fullName, 
        email: currentUser.userInfo.email ,
        phoneNumber: currentUser.userInfo.phoneNumber 
      });
    } else {
      this.toastr.warning('Could not load user details.');
    }
  }

  saveChanges() {
  if (this.profileForm.valid) {
    const currentUser = this.authService.currentUser();
    const userId = currentUser?.userInfo?.id;
    
    console.log('User ID:', userId);
    
    this.authService.updateUser(userId, this.profileForm.getRawValue()).subscribe({
      next: (res) => {
        this.toastr.success('Success: update data successfully!');
        
     
        if (currentUser && currentUser.userInfo) {
          currentUser.userInfo.fullName = this.profileForm.value.fullName!;
          currentUser.userInfo.email = this.profileForm.value.email!;
          currentUser.userInfo.phoneNumber = this.profileForm.value.phoneNumber!;
          
          
          this.authService.currentUser.set({ ...currentUser });
        }

        
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.toastr.error('Error: error to update data!');
        console.error('Server Error:', err); 
      }
    });
  } else {
    this.toastr.warning('Please make sure the data is correct!');
  }
}
}