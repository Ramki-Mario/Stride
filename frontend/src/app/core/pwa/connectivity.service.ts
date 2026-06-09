import { Injectable, signal, OnDestroy } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ConnectivityService implements OnDestroy {
  readonly isOnline = signal(typeof navigator !== 'undefined' ? navigator.onLine : true);

  private readonly onOnline  = () => this.isOnline.set(true);
  private readonly onOffline = () => this.isOnline.set(false);

  constructor() {
    window.addEventListener('online',  this.onOnline);
    window.addEventListener('offline', this.onOffline);
  }

  ngOnDestroy(): void {
    window.removeEventListener('online',  this.onOnline);
    window.removeEventListener('offline', this.onOffline);
  }
}
