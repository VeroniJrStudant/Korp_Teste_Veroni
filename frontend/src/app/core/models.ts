export interface Product {
  id: string;
  code: string;
  description: string;
  balance: number;
  createdAt: string;
  updatedAt: string;
}

export interface InvoiceItem {
  id: string;
  productId: string;
  productCode: string;
  productDescription: string;
  quantity: number;
}

export interface Invoice {
  id: string;
  number: number;
  status: 'Aberta' | 'Fechada';
  createdAt: string;
  closedAt: string | null;
  items: InvoiceItem[];
}

export interface PrintResult {
  invoice: Invoice;
  idempotentReplay: boolean;
  message: string;
}

export interface Insight {
  title: string;
  detail: string;
  severity: 'ok' | 'info' | 'warning' | 'danger';
}

export interface AssistantReply {
  answer: string;
  highlights: string[];
}

export interface ServiceHealth {
  service: string;
  status: string;
  chaos?: boolean;
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
}
