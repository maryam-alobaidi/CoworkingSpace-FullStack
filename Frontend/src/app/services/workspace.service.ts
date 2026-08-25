import { inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable, tap } from 'rxjs';
import { WorkSpace } from '../models/workspace.model';

@Injectable({providedIn:'root'})
export class WorkSpaceService{

  private apiUrl='https://localhost:7167/api/WorkspaceSpaces'

  private http=inject(HttpClient);
  workspaces=signal<WorkSpace[]>([]);


    private imageMap: { [key: string]: string } = {
      'Meeting Room': '/images/MeetingRoom.jpg',
      'Dedicated Desk': '/images/DedicatedDesk.jpg',
      'Hot Desk': '/images/HotDesk.webp',
      'Private Office': '/images/Private Office.avif'
    };

  getWorkSpace():Observable<WorkSpace[]>{
   return this.http.get<WorkSpace[]>(`${this.apiUrl}/getAll`).pipe(
      
      map(data => data.map(space => ({
        ...space,
        imageUrl:this.imageMap[space.spaceType] || 'https://picsum.photos/seed/default/600/400'
      }))),
      
      tap(mappedData => this.workspaces.set(mappedData))
    );
  }

  getWorkSpaceById(id:number):Observable<WorkSpace>{

   return this.http.get<WorkSpace>(`${this.apiUrl}/${id}`).pipe(
      map(space=>({
        ...space,imageUrl:this.imageMap[space.spaceType] || 'https://picsum.photos/seed/default/600/400'
      }))
   )
  }

   getTotalWorkSpace():Observable<WorkSpace[]>{
     return this.http.get<WorkSpace[]>(`${this.apiUrl}/getAll`);
  }


  createSpace(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/add`, data, { responseType: 'text' });
  }

  updateSpace(Id:number|null,data:any): Observable<any> {
    return this.http.put(`${this.apiUrl}/update/${Id}`, data, { responseType: 'text' });
  }


}


