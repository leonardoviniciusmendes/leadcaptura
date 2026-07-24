type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
  id: number;
  type: ToastType;
  title: string;
  message?: string;
}

interface ConfirmPayload {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  resolve: (value: boolean) => void;
}

const bus = new EventTarget();

export function showToast(input: Omit<ToastMessage, 'id'>) {
  bus.dispatchEvent(new CustomEvent('toast', { detail: { ...input, id: Date.now() } }));
}

export function confirmAction(input: Omit<ConfirmPayload, 'resolve'>): Promise<boolean> {
  return new Promise((resolve) => {
    bus.dispatchEvent(new CustomEvent('confirm', { detail: { ...input, resolve } }));
  });
}

export function onToast(handler: (toast: ToastMessage) => void) {
  const listener = (event: Event) => handler((event as CustomEvent<ToastMessage>).detail);
  bus.addEventListener('toast', listener);
  return () => bus.removeEventListener('toast', listener);
}

export function onConfirm(handler: (payload: ConfirmPayload) => void) {
  const listener = (event: Event) => handler((event as CustomEvent<ConfirmPayload>).detail);
  bus.addEventListener('confirm', listener);
  return () => bus.removeEventListener('confirm', listener);
}
