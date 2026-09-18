import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AssistantReply } from '../../core/models';
import { BillingService } from '../../core/billing.service';

@Component({
  selector: 'app-assistant',
  imports: [CommonModule, FormsModule],
  templateUrl: './assistant.component.html',
  styleUrl: './assistant.component.scss'
})
export class AssistantComponent {
  question = 'Como simular a falha do estoque?';
  reply?: AssistantReply;
  loading = false;

  constructor(private readonly billing: BillingService) {}

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
