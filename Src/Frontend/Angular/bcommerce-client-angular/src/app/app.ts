import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from './shared/components/toast/toast';
import { Header } from "./shared/components/header/header";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, Header],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('bcommerce-client-angular');
}
