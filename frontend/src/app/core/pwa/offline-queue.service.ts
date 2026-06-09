import { Injectable } from '@angular/core';

export interface QueuedCompletion {
  id:          string;   // UUID, client-generated
  instanceId:  string;
  stepId:      string;
  payload:     string;   // JSON body sent to /bff/workflows/instances/:id/steps/:sid/complete
  queuedAt:    number;   // Date.now()
}

const DB_NAME    = 'stride-offline';
const DB_VERSION = 1;
const STORE_NAME = 'step-completions';

/**
 * IndexedDB-backed queue for step completions made while offline.
 * All methods are async and handle the case where IDB is unavailable gracefully.
 */
@Injectable({ providedIn: 'root' })
export class OfflineQueueService {
  private _db: IDBDatabase | null = null;

  /** Opens (or creates) the database. Idempotent — safe to call multiple times. */
  async open(): Promise<IDBDatabase | null> {
    if (this._db) return this._db;
    if (typeof indexedDB === 'undefined') return null;

    return new Promise((resolve) => {
      const req = indexedDB.open(DB_NAME, DB_VERSION);

      req.onupgradeneeded = (e) => {
        const db = (e.target as IDBOpenDBRequest).result;
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          db.createObjectStore(STORE_NAME, { keyPath: 'id' });
        }
      };

      req.onsuccess = (e) => {
        this._db = (e.target as IDBOpenDBRequest).result;
        resolve(this._db);
      };

      req.onerror = () => resolve(null);
    });
  }

  async enqueue(item: QueuedCompletion): Promise<void> {
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

  async getAll(): Promise<QueuedCompletion[]> {
    const db = await this.open();
    if (!db) return [];
    return new Promise((resolve, reject) => {
      const tx  = db.transaction(STORE_NAME, 'readonly');
      const req = tx.objectStore(STORE_NAME).getAll();
      req.onsuccess = () => resolve((req.result as QueuedCompletion[]) ?? []);
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
