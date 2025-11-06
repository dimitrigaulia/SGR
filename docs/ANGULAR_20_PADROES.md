# Padrões Angular 20 - Sintaxe Moderna e Boas Práticas

Este documento descreve os padrões recomendados para Angular 20, incluindo a nova sintaxe de controle de fluxo e nomenclatura.

## 🆕 Nova Sintaxe de Controle de Fluxo (Angular 17+)

O Angular 17 introduziu uma nova sintaxe de controle de fluxo que substitui as diretivas estruturais tradicionais. **Esta é a sintaxe padrão e recomendada no Angular 20.**

### ⚠️ Diretivas Antigas (DEPRECIADAS)

As seguintes diretivas estão depreciadas e devem ser evitadas:
- `*ngIf` → Use `@if`
- `*ngFor` → Use `@for`
- `*ngSwitch` → Use `@switch`

### ✅ Nova Sintaxe

#### 1. `@if` (substitui `*ngIf`)

**Antes:**
```html
<div *ngIf="isLoggedIn">
  Bem-vindo!
</div>
<div *ngIf="!isLoggedIn">
  Faça login
</div>
```

**Depois:**
```html
@if (isLoggedIn) {
  <div>Bem-vindo!</div>
} @else {
  <div>Faça login</div>
}
```

**Com `@else if`:**
```html
@if (status === 'loading') {
  <app-loading></app-loading>
} @else if (status === 'error') {
  <div class="error">Erro ao carregar</div>
} @else {
  <div>Conteúdo carregado</div>
}
```

#### 2. `@for` (substitui `*ngFor`)

**Antes:**
```html
<div *ngFor="let item of items; let i = index">
  {{ i + 1 }}. {{ item.name }}
</div>
```

**Depois:**
```html
@for (item of items; track item.id) {
  <div>{{ $index + 1 }}. {{ item.name }}</div>
}
```

**Com `@empty`:**
```html
@for (item of items; track item.id) {
  <div>{{ item.name }}</div>
} @empty {
  <div>Nenhum item encontrado</div>
}
```

**Variáveis disponíveis no `@for`:**
- `$index` - índice do item (substitui `let i = index`)
- `$first` - primeiro item
- `$last` - último item
- `$even` - índice par
- `$odd` - índice ímpar
- `$count` - total de itens

**Exemplo completo:**
```html
@for (user of users(); track user.id) {
  <div>
    <span>{{ $index + 1 }}</span>
    <span>{{ user.name }}</span>
    @if ($first) {
      <span>(Primeiro)</span>
    }
    @if ($last) {
      <span>(Último)</span>
    }
  </div>
} @empty {
  <div>Nenhum usuário encontrado</div>
}
```

#### 3. `@switch` (substitui `*ngSwitch`)

**Antes:**
```html
<div [ngSwitch]="status">
  <div *ngSwitchCase="'loading'">Carregando...</div>
  <div *ngSwitchCase="'success'">Sucesso!</div>
  <div *ngSwitchDefault>Padrão</div>
</div>
```

**Depois:**
```html
@switch (status) {
  @case ('loading') {
    <div>Carregando...</div>
  }
  @case ('success') {
    <div>Sucesso!</div>
  }
  @default {
    <div>Padrão</div>
  }
}
```

## 📝 Padrões de Nomenclatura

### Componentes
- **Nome do arquivo**: kebab-case (ex: `user-form.component.ts`)
- **Nome da classe**: PascalCase (ex: `UserFormComponent`)
- **Seletor**: kebab-case com prefixo `app-` (ex: `app-user-form`)

### Services
- **Nome do arquivo**: kebab-case (ex: `usuario.service.ts`)
- **Nome da classe**: PascalCase com sufixo `Service` (ex: `UsuarioService`)
- **Interface**: PascalCase com prefixo `I` (ex: `IUsuarioService`)

### Models/Interfaces
- **Nome do arquivo**: kebab-case (ex: `usuario.model.ts`)
- **Nome da interface**: PascalCase (ex: `UsuarioDto`, `CreateUsuarioRequest`)

### Variáveis e Propriedades
- **CamelCase** para variáveis e propriedades (ex: `isLoading`, `userName`)
- **readonly** para propriedades imutáveis (ex: `readonly isLoading = signal(false)`)
- **Sufixo `$`** para Observables (opcional, mas recomendado): `users$`

### Signals
- **Sufixo `()`** ao acessar signals no template: `isLoading()`, `users()`
- **Nome descritivo**: `isLoading`, `data`, `total`, `pageIndex`

### Métodos
- **CamelCase** para métodos (ex: `loadData()`, `onSubmit()`)
- **Prefixo `on`** para handlers de eventos (ex: `onClick()`, `onSubmit()`)
- **Prefixo `handle`** para handlers complexos (ex: `handleError()`)

### Constantes
- **UPPER_SNAKE_CASE** para constantes (ex: `MAX_FILE_SIZE`, `API_BASE_URL`)

## 🎯 Padrões de Template

### Signals no Template
```html
<!-- ✅ Correto -->
@if (isLoading()) {
  <app-loading></app-loading>
}

@for (user of users(); track user.id) {
  <div>{{ user.name }}</div>
}

<!-- ❌ Errado -->
<div *ngIf="isLoading">
  <app-loading></app-loading>
</div>
```

### Formulários
```html
<form #f="ngForm" (ngSubmit)="onSubmit()">
  <mat-form-field appearance="outline" class="form-field-spacing">
    <mat-label>Nome</mat-label>
    <input matInput name="nome" [(ngModel)]="model.nome" required />
    @if (f.controls['nome']?.errors?.['required'] && f.controls['nome']?.touched) {
      <mat-error>Nome é obrigatório</mat-error>
    }
  </mat-form-field>
</form>
```

### Listas com Loading
```html
@if (isLoading()) {
  <app-loading></app-loading>
} @else {
  @if (data().length > 0) {
    @for (item of data(); track item.id) {
      <div>{{ item.name }}</div>
    }
  } @else {
    <div>Nenhum item encontrado</div>
  }
}
```

## 🔧 Padrões de Componentes

### Estrutura Básica
```typescript
import { Component, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule, /* outros imports */],
  templateUrl: './example.component.html',
  styleUrls: ['./example.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush // ✅ Sempre usar OnPush
})
export class ExampleComponent {
  // Signals
  readonly isLoading = signal(false);
  readonly data = signal<Item[]>([]);
  
  // Computed
  readonly hasData = computed(() => this.data().length > 0);
  
  // Services (inject)
  private service = inject(ExampleService);
  
  // Métodos
  loadData() {
    this.isLoading.set(true);
    // ...
  }
}
```

### Gerenciamento de Subscriptions
```typescript
import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

export class ExampleComponent {
  private destroyRef = inject(DestroyRef);
  
  constructor() {
    this.service.getData()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => this.data.set(data),
        error: (error) => console.error(error)
      });
  }
}
```

## 📱 Responsividade

### Breakpoints
```typescript
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';

readonly isMobile = signal(false);

constructor() {
  this.bp.observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(result => {
      this.isMobile.set(result.matches);
    });
}
```

### No Template
```html
@if (isMobile()) {
  <div class="mobile-view">
    <!-- Versão mobile -->
  </div>
} @else {
  <div class="desktop-view">
    <!-- Versão desktop -->
  </div>
}
```

## 🎨 Estilos

### Classes Globais
- Use classes globais de `styles.scss` quando disponíveis
- `.form-container`, `.form-field-spacing`, `.form-actions`, etc.
- Veja [GUIA_FORMULARIOS.md](./GUIA_FORMULARIOS.md) para mais detalhes

### SCSS do Componente
```scss
// Estilos específicos do componente
// Os estilos globais estão em styles.scss

.component-specific {
  // Estilos específicos aqui
}
```

## ✅ Checklist para Novos Componentes

- [ ] Usar `@if`, `@for`, `@switch` em vez de `*ngIf`, `*ngFor`, `*ngSwitch`
- [ ] Usar `ChangeDetectionStrategy.OnPush`
- [ ] Usar `signals` para estado reativo
- [ ] Usar `computed` para valores derivados
- [ ] Usar `inject()` para dependency injection
- [ ] Usar `takeUntilDestroyed()` para subscriptions
- [ ] Nomes de arquivos em kebab-case
- [ ] Nomes de classes em PascalCase
- [ ] Signals com `()` no template
- [ ] Métodos com prefixo `on` para event handlers

## 📚 Referências

- [Angular Control Flow](https://angular.dev/guide/control-flow)
- [Angular Signals](https://angular.dev/guide/signals)
- [Angular Standalone Components](https://angular.dev/guide/components/importing)
- [Angular Style Guide](https://angular.dev/style-guide)

## 🔄 Migração

Para migrar código existente:

1. Substitua `*ngIf` por `@if`
2. Substitua `*ngFor` por `@for` com `track`
3. Substitua `*ngSwitch` por `@switch`
4. Adicione `@empty` em loops quando apropriado
5. Use `$index` em vez de `let i = index`
6. Atualize imports se necessário (CommonModule pode não ser necessário)

