import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AssistantReply } from '../../core/models';
import { BillingService } from '../../core/services/billing.service';

@Component({
  selector: 'app-assistant',
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './assistant.component.html',
  styleUrl: './assistant.component.scss'
})
export class AssistantComponent {
  question = 'Como simular a falha do estoque?';
  reply?: AssistantReply;
  loading = false;

  constructor(private readonly billing: BillingService) {}

  askFor(question: string): void {
    this.question = question;
    this.ask();
  }

  ask(): void {
    this.loading = true;
    this.billing.ask(this.question).subscribe({
      next: reply => {
        this.reply = reply;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }
}
