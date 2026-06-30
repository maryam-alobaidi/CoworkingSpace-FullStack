import { Component, inject } from '@angular/core';
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
export class EditProfile {

  public authService=inject(Auth);
  private toastr=inject(ToastrService);
  private router=inject(Router);

 profileForm=new FormGroup({
    fullName:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.minLength(3)]}),
    email:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.email]}),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required] }) 
  })
  


 saveChanges() {


  if (this.profileForm.valid) {
    const currentUser = this.authService.currentUser();
 
  
  const userId = currentUser?.userInfo?.id;
  console.log('User ID:', userId);
    this.authService.updateUser(userId, this.profileForm.value).subscribe({
      next: (res) => {
        this.toastr.success('Success: update data successfully!');
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
