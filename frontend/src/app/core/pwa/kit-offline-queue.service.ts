import { Injectable } from '@angular/core';

export type KitActionType = 'checkout' | 'return';

export interface QueuedKitAction {
  id:           string;          // client-generated UUID
  type:         KitActionType;
  kitItemId?:   string;          // present for checkout
  kitItemName?: string;          // display label
  checkoutId?:  string;          // present for return
  payload:      string;          // JSON body sent to the BFF
  queuedAt:     number;          // Date.now()
}

const DB_NAME    = 'stride-kitops-offline';
const DB_VERSION = 1;
const STORE_NAME = 'kit-actions';

/**
 * IndexedDB-backed queue for KitOps checkout / return actions made while offline.
 * Uses its own database so it never collides with the workflow step-completion queue.
 */
@Injectable({ providedIn: 'root' })
export class KitOfflineQueueService {
  private _db: IDBDatabase | null = null;

  async open(): Promise<IDBDatabase | null> {
    if (this._db) return this._db;
    if (typeof indexedDB === 'undefined') return null;

    return new Promise((resolve) => {
      const req = indexedDB.open(DB_NAME, DB_VERSION);

      req.onupgradeneeded = (e) => {
        const db = (e.target as IDBOpenDBRequest).result;
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          const store = db.createObjectStore(STORE_NAME, { keyPath: 'id' });
          store.createIndex('queuedAt', 'queuedAt');
        }
      };

      req.onsuccess = (e) => {
        this._db = (e.target as IDBOpenDBRequest).result;
        resolve(this._db);
      };

      req.onerror = () => resolve(null);
    });
  }

  async enqueue(item: QueuedKitAction): Promise<void> {
    const db = await this.open();
    if (!db) return;
    return new Promise((resolve, reject) => {
      const tx  = db.transaction(STORE_NAME, 'readwrite');
      const req = tx.objectStore(STORE_NAME).put(item);
      req.onsuccess = () => resolve();
      req.onerror   = () => reject(req.error);
    });
  }

  async dequeue(id: string): Promise<void> {
    const db = await this.open();
    if (!db) return;
    return new Promise((resolve, reject) => {
      const tx  = db.transaction(STORE_NAME, 'readwrite');
      const req = tx.objectStore(STORE_NAME).delete(id);
      req.onsuccess = () => resolve();
      req.onerror   = () => reject(req.error);
    });
  }

  /** Returns all queued actions ordered by `queuedAt` ascending (FIFO). */
  async getAll(): Promise<QueuedKitAction[]> {
    const db = await this.open();
    if (!db) return [];
    return new Promise((resolve, reject) => {
      const tx    = db.transaction(STORE_NAME, 'readonly');
      const index = tx.objectStore(STORE_NAME).index('queuedAt');
      const req   = index.getAll();
      req.onsuccess = () => resolve((req.result as QueuedKitAction[]) ?? []);
      req.onerror   = () => reject(req.error);
    });
  }

  async count(): Promise<number> {
    const db = await this.open();
    if (!db) return 0;
    return new Promise((resolve, reject) => {
      const tx  = db.transaction(STORE_NAME, 'readonly');
      const req = tx.objectStore(STORE_NAME).count();
      req.onsuccess = () => resolve(req.result as number);
      req.onerror   = () => reject(req.error);
    });
  }
}
