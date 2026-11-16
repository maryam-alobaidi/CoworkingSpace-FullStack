import { Routes } from '@angular/router';
import { WorkspaceList } from './components/workspace-list/workspace-list';
import { About } from './components/about/about';
import { Login } from './components/login/login';
import { Register } from './components/register/register';
import { EventsList } from './components/events-list/events-list';
import { Home } from './components/home/home';
import { Profile } from './components/profile/profile';
import { EditProfile } from './components/edit-profile/edit-profile';
import { Booking } from './components/space-book/booking';
import { authGuard } from './guards/auth-guard';
import { adminGuard } from './guards/admin-guard';
import { PaymentSuccess} from './components/payment-success/payment-success';
import { OfficeBookings } from './dashboard/office-bookings/office-bookings';
import { EventTickets } from './dashboard/event-tickets/event-tickets';
import { PaymentFailed } from './components/payment-failed/payment-failed';
import { EventBook } from './components/event-book/event-book';
import { AdminLayout } from './admin/admin-layout/admin-layout';
import { AdminOverview } from './admin/admin-overview/admin-overview';
import { AdminUsers } from './admin/admin-users/admin-users';
import { AdminSpaces } from './admin/admin-spaces/admin-spaces';
import { AdminEvents } from './admin/admin-events/admin-events';



export const routes: Routes = [
    { path: '', component: Home },//home
    { path: 'about', component: About },//about
    { path: 'login', component: Login },
    { path: 'register', component: Register },
    { path: 'events', component: EventsList },
    { path: 'workspace', component: WorkspaceList },
    { path: 'profile', component: Profile, canActivate: [authGuard] },
    { path: 'edit-profile', component: EditProfile, canActivate: [authGuard] },
    { path: 'book/:id', component:Booking, canActivate: [authGuard]},//the guard is here for guard the system
    { path: 'payment-success', component: PaymentSuccess },
    { path: 'dashboard/office-bookings', component: OfficeBookings, canActivate: [authGuard] },
    { path: 'dashboard/event-tickets', component: EventTickets, canActivate: [authGuard] },
    { path: 'payment-failed' , component: PaymentFailed},
    { path: 'event-book/:id' , component:EventBook, canActivate: [authGuard] },

    {
        path: 'admin',
        component: AdminLayout,
        canActivate: [adminGuard], // الهيكل الخارجي (Navbar + Sidebar)
        children: [
            { path: '', redirectTo: 'overview', pathMatch: 'full' }, 
            { path: 'overview', component: AdminOverview } ,
            { path: 'users', component: AdminUsers } ,
            { path: 'spaces', component: AdminSpaces } ,
            { path: 'events', component: AdminEvents } ,
        ]
    }
];
