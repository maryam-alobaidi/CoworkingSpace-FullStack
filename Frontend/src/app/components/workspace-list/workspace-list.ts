import { Component, inject, OnInit } from '@angular/core';
import { WorkSpaceService } from '../../services/workspace.service';

import {CommonModule,SlicePipe} from '@angular/common'
import { RouterLink } from "@angular/router";

@Component({
  selector: 'app-workspace-list',
  imports: [CommonModule, SlicePipe, RouterLink],
  templateUrl: './workspace-list.html',
  styleUrl: './workspace-list.scss',
})
export class WorkspaceList implements OnInit {
public workSpaceService = inject(WorkSpaceService);

  ngOnInit(): void {
   
    this.workSpaceService.getWorkSpace().subscribe({
      error: (err) => console.error('Error fetching data:', err)
    });
  }
}

