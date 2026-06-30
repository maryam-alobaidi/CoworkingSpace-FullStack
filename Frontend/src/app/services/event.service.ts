import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { map, Observable, tap } from 'rxjs';
import { EventModel } from '../models/event.model';

@Injectable({
  providedIn: 'root',
})
export class EventSevice {

  private apiUrl='https://localhost:7167/api/Events'

  private http=inject(HttpClient);
  events=signal<EventModel[]>([]);

      imageMap: { [key: number]: string } = {
      1: '/images/eventSpace1.jpeg',
      2: '/images/eventSpace2.avif',
      3: '/images/eventSpace3.jpg',
      4: '/images/eventSpace4.webp',
      5: '/images/eventSpace5.webp',
      6: '/images/eventSpace6.jpg',
      7: '/images/eventSpace7.jpg',
      8: '/images/eventSpace8.jpg',
      9: '/images/eventSpace9.jpg',
      };

  getAllEvents():Observable<EventModel[]>{

 
    

    return this.http.get<EventModel[]>(`${this.apiUrl}/getAll`).pipe(
         map(data => data.map(ev => {
       
        const randomId = Math.floor(Math.random() * 9) + 1;

        return {
          ...ev,
          
          imageUrl:this.imageMap[randomId] || 'https://picsum.photos/seed/default/600/400'
        };
      })),
      
      tap(data => this.events.set(data))
    );  

  }
   

getEventDetailsById(id: any): Observable<EventModel> {
  return this.http.get<EventModel>(`${this.apiUrl}/get/${id}`).pipe(
    map((ev: any) => {
     
      const num = Math.floor(Math.random() * 9) + 1;

     
      return {
        ...ev,
        imageUrl: this.imageMap[num] 
      } as EventModel;
    })
  );
}




}
