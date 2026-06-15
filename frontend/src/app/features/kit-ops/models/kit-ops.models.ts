export interface KitItemSummaryDto {
  id:            string;
  name:          string;
  category:      string;
  description:   string | null;
  totalQuantity: number;
  isActive:      boolean;
  createdAt:     string;
}

export interface KitItemDetailDto {
  id:            string;
  name:          string;
  category:      string;
  description:   string | null;
  totalQuantity: number;
  isActive:      boolean;
  createdAt:     string;
  updatedAt:     string;
}

export interface KitItemAvailabilityDto {
  kitItemId:            string;
  name:                 string;
  totalQuantity:        number;
  outstandingCheckouts: number;
  availableQuantity:    number;
  overdueCheckouts:     number;
}

export interface PagedResult<T> {
  items:      T[];
  totalCount: number;
  page:       number;
  pageSize:   number;
  totalPages: number;
}

export interface CreateKitItemRequest {
  name:          string;
  category:      string;
  description:   string | null;
  totalQuantity: number;
}

export interface UpdateKitItemRequest {
  name:          string;
  category:      string;
  description:   string | null;
  totalQuantity: number;
}

// ── Field user DTOs ───────────────────────────────────────────────────────────

export interface KitCatalogItemDto {
  kitItemId:            string;
  name:                 string;
  category:             string;
  description:          string | null;
  totalQuantity:        number;
  outstandingCheckouts: number;
  availableQuantity:    number;
  overdueCheckouts:     number;
}

export interface MyKitCheckoutDto {
  checkoutId:       string;
  kitItemId:        string;
  kitItemName:      string;
  category:         string;
  checkedOutAt:     string;
  expectedReturnAt: string;
  returnedAt:       string | null;
  notes:            string | null;
  statusLabel:      string;
  statusValue:      number;
}

export interface MyKitReservationDto {
  reservationId: string;
  kitItemId:     string;
  kitItemName:   string;
  category:      string;
  requestedAt:   string;
  notes:         string | null;
  statusLabel:   string;
  statusValue:   number;
}

export interface CheckoutKitItemRequest {
  kitItemId: string;
  days:      number;
  notes:     string | null;
}

export interface CreateKitReservationRequest {
  kitItemId: string;
  notes:     string | null;
}
