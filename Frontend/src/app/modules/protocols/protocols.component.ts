import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-protocols',
  standalone: false,
  templateUrl: './protocols.component.html',
  styleUrl: './protocols.component.css'
})
export class ProtocolsComponent {
  @Input() role = '';
}
