import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { Navbar } from './components/navbar/navbar';
import { Footer } from "./components/footer/footer";
import { filter } from 'rxjs';
import { CommonModule } from '@angular/common';
import { AiChat } from "./components/ai-chat/ai-chat";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, Footer, CommonModule, AiChat],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('Frontend');
  private router = inject(Router);

  isAdminRoute=signal(false);

  constructor(){
    this.router.events.pipe(
      filter(ev=> ev instanceof NavigationEnd)
    ).subscribe((ev:any)=>{
      this.isAdminRoute.set(ev.url.startsWith('/admin'));
    
    });
  }


}
