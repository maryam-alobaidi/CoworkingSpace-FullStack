import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from '../services/auth';

// UX-only protection for /admin; the API enforces the Admin role itself.
export const adminGuard: CanActivateFn = () => {
  const authService = inject(Auth);
  const router = inject(Router);

  const user = authService.currentUser();
  if (!user) {
    return router.createUrlTree(['/login']);
  }

  const role = user.userInfo.role?.toString().toLowerCase();
  return role === 'admin' ? true : router.createUrlTree(['/']);
};
