import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { validate } from '@angular/forms/signals';
import { Auth } from '../../services/auth';
import { Router, RouterLink } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {

  private authService=inject(Auth);
  private router=inject(Router);
  toastr=inject(ToastrService);


  loginForm=new FormGroup({
    email:new FormControl('',{
      nonNullable:true,
      validators:[Validators.required,Validators.email]
    }),
    password:new FormControl('',{
      nonNullable:true,
      validators:[Validators.required,Validators.minLength(8)]
    })
  });


  onSubmit() {
    if (this.loginForm.valid) {
    const loginData = this.loginForm.getRawValue();
    
    this.authService.login(loginData).subscribe({
      next: (response) => {
        
        const isSuspended = response?.isSuspended;

        if (!isSuspended) {
         
          const storedUserData = localStorage.getItem('user_data');
          const parsedUser = storedUserData ? JSON.parse(storedUserData) : null;
          
          const userRole = response?.role || response?.Role || parsedUser?.role;
        
          if (userRole && userRole.toLowerCase() === 'admin') {
            this.router.navigate(['/admin']);
          } else {
            this.router.navigate(['/']);
          }
        } else {
          
          this.toastr.error("User is not active. Contact with the service.");
        }
      },
      error: (err) => {
        this.toastr.error("Invalid credentials or server error.");
      }
    });
    }
  }
}
