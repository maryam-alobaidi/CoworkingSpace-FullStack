export interface EventModel {
currentUserId: any;

id:number;
title:string;
description:string;
eventDate:Date|string;
ticketPrice:number;
maxAttendees:number;
availableSeats:number;
imageUrl?: string;
}
