export interface UserWithRoleModel {
  userId: number;    
  fullName: string;
  email: string;   
  phoneNumber:string;             
  roleId: number;    
  joinDate: string;
  isSuspended: boolean;
}