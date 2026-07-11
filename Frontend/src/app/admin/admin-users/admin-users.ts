import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { UserWithRoleModel } from '../../models/user-with-role-model';
import { Auth } from '../../services/auth';
import { CommonModule, DatePipe, JsonPipe, NgClass } from '@angular/common';
import { FormBuilder, FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';

export enum UserRole {
  Admin = 4,
  Member = 5
}

@Component({
  selector: 'app-admin-users',
  imports: [NgClass,DatePipe,CommonModule, FormsModule,ReactiveFormsModule],
  templateUrl: './admin-users.html',
  styleUrl: './admin-users.scss',
})
export class AdminUsers implements OnInit {
 
  readonly Role=UserRole;

  authService=inject(Auth);
  toastr=inject(ToastrService);
  private fb=inject(FormBuilder);
  

  userForm = this.fb.group({
    fullName:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.minLength(3)]}),
    email:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.email]}),
    password:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.minLength(8)]}),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    roleId: [5, Validators.required]
  });

   
  editForm=this.fb.group({
    fullName:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.minLength(3)]}),
    email:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.email]}),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  })

  usersList = signal<UserWithRoleModel[]>([]);
  selectedUser=signal<UserWithRoleModel|null>(null);
  searchTerm=signal<string>("");
  statusFilter = signal<string>('all');
  roleFilter = signal<string>('all');

 ngOnInit(): void {
   this.loadUsers();
  }

  loadUsers(){
    this.authService.getAllUsersWithRoles().subscribe({
      next:(data:UserWithRoleModel[])=>{
        this.usersList.set(data);
      },
      error:(err)=>{
         console.error('Error getting all users !', err);
      }
    })
  }

  countUsersByRole(roleId:number){
    return this.usersList().filter(u=>u.roleId===roleId).length;
  }

  onSaveUser(){
    if(this.userForm.invalid){
      this.toastr.error('Please fill in all required fields.');
      this.userForm.markAllAsTouched();
      return;
    }
    const userData=this.userForm.getRawValue();

    this.authService.register(userData).subscribe({
       next:()=>{
          this.toastr.success('User Added Successfully! ');
          this.resetForm();
          this.loadUsers();
         document.getElementById('closeModalButton')?.click();

        },
        error: (err) => {
        const errorMsg = err.error?.message || err.error || 'Something went wrong';
      this.toastr.error('Addition failed: ' + errorMsg);
      }
      })
  }

  resetForm(){
    this.userForm.reset({ roleId: 5 });
  }

  onEditUser(user:UserWithRoleModel){
    this.selectedUser.set(user);

    this.editForm.patchValue({
    fullName: user.fullName,
    email: user.email,
    phoneNumber:user.phoneNumber
  });
  }

  onSaveEditUser() {
   
    if (this.editForm.invalid || !this.selectedUser()) {
      this.toastr.error('Please fill in all required fields correctly.');
      this.editForm.markAllAsTouched();
      return;
    }
 
    this.authService.updateUser(this.selectedUser()?.userId, this.editForm.getRawValue()).subscribe({
      next: () => {
        this.toastr.success('User updated successfully!');
        this.loadUsers();
      
        document.getElementById('closeEditModalButton')?.click(); 
      },
      error: (err) => {
        const errorMsg = err.error?.message || err.error || 'Something went wrong';
        this.toastr.error('Update failed: ' + errorMsg);
      }
    });
  }

  suspendUser(user: UserWithRoleModel) {
  this.authService.toggleSuspend(user.userId).subscribe({
    next: (response) => {
      const willSuspend = !user.isSuspended;
      
      if (willSuspend) {
        this.toastr.success("User account has been suspended successfully.");
      } else {
        this.toastr.success("User account has been activated successfully.");
      }
    },
    error: (err) => {
      console.error(err);
      this.toastr.error("An error occurred while updating user status.");
    }
  });
  }


  // for User monitoring

  filteredUsers = computed(() => {
  const term = this.searchTerm().trim().toLowerCase();
  const status = this.statusFilter();
  const role = this.roleFilter();
  
  return this.usersList().filter(u => {
   
    const matchesSearch = !term || 
                          u.fullName.toLowerCase().includes(term) || 
                          u.email.toLowerCase().includes(term);

   
    const matchesStatus = status === 'all' || 
                          (status === 'active' && !u.isSuspended) || 
                          (status === 'suspended' && u.isSuspended);

   
    const matchesRole = role === 'all' || 
                        (role === 'admin' && u.roleId === this.Role.Admin) || 
                        (role === 'member' && u.roleId === this.Role.Member);

   
    return matchesSearch && matchesStatus && matchesRole;
  });
  });
  
  

  onSearchChange(event: Event){
    const value=(event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
  }

  onStatusFilterChange(event:Event){
    const value=(event.target as HTMLSelectElement).value;
    this.statusFilter.set(value);
  }

  onRoleFilterChange(event:Event){
    const value = (event.target as HTMLSelectElement).value;
    this.roleFilter.set(value);
  }


}
