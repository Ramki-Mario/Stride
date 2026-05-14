import { HttpInterceptorFn } from '@angular/common/http';
import { v4 as uuidv4 } from 'uuid';

export const correlationInterceptor: HttpInterceptorFn = (req, next) => {
  const correlationId = uuidv4();
  return next(req.clone({ setHeaders: { 'X-Correlation-Id': correlationId } }));
};
