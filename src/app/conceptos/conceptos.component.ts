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
} from '@infragistics/igniteui-angular';
import { IGX_INPUT_GROUP_DIRECTIVES } from '@infragistics/igniteui-angular/input-group';
import { IgxIconComponent } from '@infragistics/igniteui-angular/icon';

interface ConceptoMock {
  id: number;
  descripcion: string;
}

@Component({
  selector: 'app-conceptos',
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
  templateUrl: './conceptos.component.html',
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
export class ConceptosComponent {
  @ViewChild('grid', { static: false })
  grid?: IgxGridComponent;

  @ViewChild('dialog', { static: false })
  dialog?: IgxDialogComponent;

  @ViewChild('deleteDialog', { static: false })
  deleteDialog?: IgxDialogComponent;

  data: ConceptoMock[] = [
    { id: 1, descripcion: 'Honorarios' },
    { id: 2, descripcion: 'Gastos Administrativos' },
    { id: 3, descripcion: 'Recargo Financiero' },
    { id: 4, descripcion: 'Descuento Comercial' },
    { id: 5, descripcion: 'Ajuste Manual' }
  ];

  editingId: number | null = null;
  selectedId: number | null = null;
  pendingDeleteId: number | null = null;

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

  openEditDialog(item: ConceptoMock): void {
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

  delete(item: ConceptoMock): void {
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

  onRowSelectionChanging(event: any): void {
    const nextSelection = event?.newSelection as Array<number | string> | undefined;
    this.selectedId = nextSelection && nextSelection.length > 0 ? Number(nextSelection[0]) : null;
  }

  onRowClick(event: any): void {
    const rowData =
      event?.rowData ??
      event?.cell?.row?.data ??
      event?.row?.data;

    if (!rowData?.id) {
      return;
    }

    this.grid?.selectRows([rowData.id], true);
    this.selectedId = rowData.id;
  }

  onGridDoubleClick(event: any): void {
    const rowData =
      event?.cell?.row?.data ??
      event?.row?.data ??
      event?.owner?.selectedRows?.[0];

    if (!rowData?.id) {
      return;
    }

    this.selectedId = rowData.id;
    this.openEditDialog(rowData);
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

    this.pendingDeleteId = selected.id;
    this.deleteDialog?.open();
  }

  confirmDelete(): void {
    const selected = this.data.find(item => item.id === this.pendingDeleteId);
    if (!selected) {
      this.pendingDeleteId = null;
      this.deleteDialog?.close();
      return;
    }

    this.delete(selected);
    this.pendingDeleteId = null;
    this.deleteDialog?.close();
  }

  cancelDelete(): void {
    this.pendingDeleteId = null;
    this.deleteDialog?.close();
  }

  private getSelectedItem(): ConceptoMock | undefined {
    if (this.selectedId === null) {
      return undefined;
    }

    return this.data.find(item => item.id === this.selectedId);
  }

  get pendingDeleteItem(): ConceptoMock | undefined {
    if (this.pendingDeleteId === null) {
      return undefined;
    }

    return this.data.find(item => item.id === this.pendingDeleteId);
  }
}
