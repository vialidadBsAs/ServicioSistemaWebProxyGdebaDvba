import { CommonModule } from '@angular/common';
import { Component, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  IgxButtonModule,
  IgxDialogComponent,
  IgxDialogModule,
  IgxGridComponent,
  IgxGridModule,
  IgxRippleModule
} from '@infragistics/ignite-ui-angular';
import { IGX_INPUT_GROUP_DIRECTIVES } from '@infragistics/ignite-ui-angular/input-group';
import { IgxIconComponent } from '@infragistics/ignite-ui-angular/icon';

interface CategoriaMock {
  id: number;
  descripcion: string;
}

@Component({
  selector: 'app-reference-demo',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    IgxGridModule,
    IgxDialogModule,
    IGX_INPUT_GROUP_DIRECTIVES,
    IgxButtonModule,
    IgxIconComponent,
    IgxRippleModule
  ],
  templateUrl: './reference-demo.component.html',
  styles: [`
    .master-page {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      padding: 1.5rem 2rem;
      background: linear-gradient(180deg, #f7f8fb 0%, #edf2f8 100%);
      min-height: 100vh;
      box-sizing: border-box;
      font-family: "Segoe UI", sans-serif;
    }

    .master-page__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
    }

    .master-page__title {
      margin: 0;
      font-size: 1.75rem;
      font-weight: 700;
      color: #1f2937;
    }

    .master-page__subtitle {
      margin: 0.25rem 0 0;
      color: #6b7280;
      font-size: 0.95rem;
    }

    .master-card {
      background: #fff;
      border: 1px solid #e5e7eb;
      border-radius: 18px;
      box-shadow: 0 10px 28px rgba(15, 23, 42, 0.08);
      padding: 1rem;
    }

    .grid-toolbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 0.75rem;
      gap: 1rem;
    }

    .grid-toolbar__meta {
      color: #4b5563;
      font-size: 0.9rem;
    }

    .dialog-title {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      font-weight: 600;
    }

    .master-form {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      min-width: 420px;
      padding-top: 0.5rem;
    }

    .field-error {
      color: #b91c1c;
      font-size: 0.85rem;
      margin-top: 0.25rem;
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      width: 100%;
    }

    @media (max-width: 768px) {
      .master-page {
        padding: 1rem;
      }

      .master-page__header {
        flex-direction: column;
        align-items: stretch;
      }

      .master-form {
        min-width: auto;
      }
    }
  `]
})
export class ReferenceDemoComponent {
  @ViewChild('grid', { static: false })
  grid?: IgxGridComponent;

  @ViewChild('dialog', { static: false })
  dialog?: IgxDialogComponent;

  data: CategoriaMock[] = [
    { id: 1, descripcion: 'Categoría General' },
    { id: 2, descripcion: 'Insumos' },
    { id: 3, descripcion: 'Servicios' },
    { id: 4, descripcion: 'Promociones' },
    { id: 5, descripcion: 'Bonificaciones' }
  ];

  editingId: number | null = null;
  selectedId: number | null = null;

  readonly form = new FormGroup({
    id: new FormControl<number | null>({ value: null, disabled: true }),
    descripcion: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(80)]
    })
  });

  openNewDialog(): void {
    this.editingId = null;
    this.selectedId = null;
    this.form.reset({
      id: null,
      descripcion: ''
    });
    this.dialog?.open();
  }

  openEditDialog(item: CategoriaMock): void {
    this.editingId = item.id;
    this.form.reset({
      id: item.id,
      descripcion: item.descripcion
    });
    this.dialog?.open();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload = {
      id: this.editingId ?? this.data.length + 1,
      descripcion: value.descripcion.trim()
    };

    if (this.editingId) {
      this.data = this.data.map(item => item.id === this.editingId ? payload : item);
    } else {
      this.data = [...this.data, payload];
    }

    this.selectedId = payload.id;
    this.dialog?.close();
    this.editingId = null;
  }

  delete(item: CategoriaMock): void {
    this.data = this.data.filter(current => current.id !== item.id);
    if (this.editingId === item.id) {
      this.cancel();
    }
    if (this.selectedId === item.id) {
      this.selectedId = null;
    }
  }

  cancel(): void {
    this.editingId = null;
    this.dialog?.close();
  }

  onRowClick(event: any): void {
    const rowData =
      event?.rowData ??
      event?.cell?.row?.data ??
      event?.row?.data;

    if (!rowData?.id) {
      return;
    }

    this.selectedId = rowData.id;
  }

  editSelected(): void {
    const selected = this.getSelectedItem();
    if (!selected) {
      return;
    }

    this.openEditDialog(selected);
  }

  deleteSelected(): void {
    const selected = this.getSelectedItem();
    if (!selected) {
      return;
    }

    this.delete(selected);
  }

  private getSelectedItem(): CategoriaMock | undefined {
    if (this.selectedId === null) {
      return undefined;
    }

    return this.data.find(item => item.id === this.selectedId);
  }
}
