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
