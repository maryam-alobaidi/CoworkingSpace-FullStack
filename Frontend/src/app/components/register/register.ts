import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Auth } from '../../services/auth';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule,RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {

  private authService=inject(Auth);
  private router=inject(Router);

  signUpForm= new FormGroup({
    fullName:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.minLength(3)]}),
    email:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.email]}),
    password:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.minLength(8)]}),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required] }) 
  })


  onSubmit(){
    if(this.signUpForm.valid){
      this.authService.register(this.signUpForm.getRawValue()).subscribe({
        next:(response)=>{
          alert('Registration Successful! Please Login.');
          this.router.navigate(['/login']);
        },
        error: (err) => {
        alert('Registration failed: ' + err.error);
      }
      })
    }
  }
}
