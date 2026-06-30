export interface WorkSpace{
  id: number;
  title: string;
  description: string;
  spaceType: string;
  pricePerHour: number;
  pricePerDay: number;
  capacity: number;
  isAvailable: boolean;
  imageUrl?: string;
}