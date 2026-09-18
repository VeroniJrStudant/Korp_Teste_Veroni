import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      { path: '', pathMatch: 'full', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'produtos', loadComponent: () => import('./features/products/products.component').then(m => m.ProductsComponent) },
      { path: 'notas', loadComponent: () => import('./features/invoices/invoices.component').then(m => m.InvoicesComponent) },
      { path: 'notas/nova', loadComponent: () => import('./features/invoices/invoice-editor.component').then(m => m.InvoiceEditorComponent) },
      { path: 'notas/:id', loadComponent: () => import('./features/invoices/invoice-editor.component').then(m => m.InvoiceEditorComponent) },
      { path: 'assistente', loadComponent: () => import('./features/assistant/assistant.component').then(m => m.AssistantComponent) }
    ]
  }
];
