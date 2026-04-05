import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { NotificationService } from '../../../core/services/notification.service';
import { toastSlide } from '../../../core/animations/app.animations';

@Component({
  selector: 'app-toast-notification',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  animations: [toastSlide],
  templateUrl: './toast-notification.component.html',
  styleUrl: './toast-notification.component.scss'
})
export class ToastNotificationComponent {
  readonly notify = inject(NotificationService);

  iconFor(type: string): string {
    return { success: 'check_circle', error: 'error', info: 'info' }[type] ?? 'info';
  }
}
