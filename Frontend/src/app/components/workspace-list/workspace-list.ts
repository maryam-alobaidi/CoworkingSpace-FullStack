import { Component, inject, OnInit, signal } from '@angular/core';
import { WorkSpaceService } from '../../services/workspace.service';

import {CommonModule,SlicePipe} from '@angular/common'
import { RouterLink } from "@angular/router";
import { WorkSpace } from '../../models/workspace.model';

@Component({
  selector: 'app-workspace-list',
  imports: [CommonModule, SlicePipe, RouterLink],
  templateUrl: './workspace-list.html',
  styleUrl: './workspace-list.scss',
})
export class WorkspaceList implements OnInit {
public workSpaceService = inject(WorkSpaceService);

workSpaceList=signal<any|null>(null);

  ngOnInit(): void {
   
    this.workSpaceService.getWorkSpace().subscribe({
      next:(data)=>{
        const availableSpaces = data.filter(space => space.isAvailable === true);
        this.workSpaceList.set(availableSpaces)
      },
      error: (err) => console.error('Error fetching data:', err)
    });
  }
}

