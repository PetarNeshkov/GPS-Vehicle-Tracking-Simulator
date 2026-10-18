import { Component } from '@angular/core';
import { APP_NAME } from './core/constants/app.constants';

@Component({
  selector: 'app-root',
  standalone: false,
  templateUrl: './app.component.html'
})
export class AppComponent {
  protected readonly appName = APP_NAME;
}
