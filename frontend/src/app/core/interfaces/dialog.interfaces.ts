// Dialog-related interfaces
import { ComponentType } from '@angular/cdk/overlay';

export interface DialogConfig<T = any> {
  width?: string;
  height?: string;
  maxWidth?: string;
  maxHeight?: string;
  disableClose?: boolean;
  data?: T;
}

export interface DialogResult<T = any> {
  action: 'save' | 'cancel' | 'delete';
  data?: T;
}

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  isDangerous?: boolean;
}

export interface FormDialogData<T = any> {
  title: string;
  data?: T;
  mode: 'create' | 'edit';
}

// Table-related interfaces
export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  type?: 'text' | 'number' | 'date' | 'currency' | 'boolean' | 'actions';
}

export interface TableAction {
  icon: string;
  label: string;
  action: string;
  color?: 'primary' | 'accent' | 'warn';
  disabled?: (item: any) => boolean;
}

export interface SortConfig {
  column: string;
  direction: 'asc' | 'desc';
}
