import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { validate } from '@angular/forms/signals';
import { Auth } from '../../services/auth';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {

  private authService=inject(Auth);
  private router=inject(Router);

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



  onSubmit(){
    if(this.loginForm.valid){
      const loginData=this.loginForm.getRawValue();
     this.authService.login(loginData).subscribe({
       next:(response)=>{
       console.log("todo perfecto..");
        this.router.navigate(['/']);
       },
       error: (err) => {
        console.error('Error for login:', err);
       
        alert('Email or Password incorrect');
      }
     })
    }

  }
}
