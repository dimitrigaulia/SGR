# Guia de Formulários - Classes Globais Reutilizáveis

Este guia mostra como usar as classes globais de formulário disponíveis em `styles.scss` para criar formulários consistentes e responsivos em todo o sistema.

## 📋 Classes Disponíveis

Todas as classes abaixo estão disponíveis globalmente e podem ser usadas em qualquer componente sem importação adicional.

### `.form-container`
Container principal do formulário com padding, max-width e centralização.

```html
<div class="form-container">
  <!-- Conteúdo do formulário -->
</div>
```

**Características:**
- Padding: 16px (12px no mobile)
- Max-width: 800px
- Centralizado automaticamente
- Responsivo para tablets e mobile

### `.form-title`
Título do formulário com espaçamento e tamanho padronizados.

```html
<h2 class="form-title">Cadastro de Usuário</h2>
```

**Características:**
- Font-size: 1.75rem (1.5rem tablet, 1.25rem mobile)
- Margin-bottom: 32px (24px tablet, 20px mobile)
- Font-weight: 500

### `.form-field-spacing`
Aplicar em `mat-form-field` para espaçamento consistente entre campos.

```html
<mat-form-field appearance="outline" class="form-field-spacing">
  <mat-label>Nome</mat-label>
  <input matInput name="nome" [(ngModel)]="model.nome" required />
</mat-form-field>
```

**Características:**
- Width: 100%
- Margin-bottom: 24px (20px tablet, 16px mobile)
- Display: block

### `.form-toggle-wrapper`
Wrapper para `mat-slide-toggle` com espaçamento consistente.

```html
<div class="form-toggle-wrapper">
  <mat-slide-toggle name="isAtivo" [(ngModel)]="model.isAtivo">Ativo</mat-slide-toggle>
</div>
```

**Características:**
- Margin-bottom: 24px (20px tablet, 16px mobile)
- Padding: 8px 0 (4px 0 no mobile)
- Alinhamento vertical centralizado

### `.form-section`
Agrupar seções relacionadas do formulário.

```html
<div class="form-section">
  <h3>Informações de Contato</h3>
  <!-- Campos relacionados -->
</div>
```

**Características:**
- Margin-bottom: 32px
- Útil para separar grupos de campos

### `.form-actions`
Container para botões de ação (Salvar, Cancelar, etc.).

```html
<div class="form-actions">
  <button mat-stroked-button type="button" routerLink="/lista">Voltar</button>
  <button mat-raised-button color="primary" type="submit">Salvar</button>
</div>
```

**Características:**
- Display: flex
- Gap: 12px (8px no mobile)
- Justify-content: flex-end
- Margin-top: 32px (24px no mobile)
- No mobile: botões em coluna, full-width

### `.form-error`
Mensagem de erro do formulário.

```html
<div class="form-error" *ngIf="error()">{{ error() }}</div>
```

**Características:**
- Cor: var(--mat-error)
- Background: var(--mat-error-container)
- Padding: 12px 16px
- Border-radius: 4px
- Font-size: 0.875rem

## 📝 Exemplo Completo

```html
<div class="form-container">
  <h2 class="form-title">Cadastro de Nova Entidade</h2>

  <form #f="ngForm" (ngSubmit)="save()">
    <!-- Campo de texto -->
    <mat-form-field appearance="outline" class="form-field-spacing">
      <mat-label>Nome</mat-label>
      <input matInput name="nome" [(ngModel)]="model.nome" required />
      <mat-error *ngIf="f.controls['nome']?.errors?.['required']">
        Nome é obrigatório
      </mat-error>
    </mat-form-field>

    <!-- Campo de email -->
    <mat-form-field appearance="outline" class="form-field-spacing">
      <mat-label>Email</mat-label>
      <input matInput type="email" name="email" [(ngModel)]="model.email" required />
      <mat-error *ngIf="f.controls['email']?.errors?.['required']">
        Email é obrigatório
      </mat-error>
    </mat-form-field>

    <!-- Toggle -->
    <div class="form-toggle-wrapper">
      <mat-slide-toggle name="isAtivo" [(ngModel)]="model.isAtivo">
        Ativo
      </mat-slide-toggle>
    </div>

    <!-- Seção relacionada -->
    <div class="form-section">
      <h3>Informações Adicionais</h3>
      <mat-form-field appearance="outline" class="form-field-spacing">
        <mat-label>Descrição</mat-label>
        <textarea matInput name="descricao" [(ngModel)]="model.descricao" rows="4"></textarea>
      </mat-form-field>
    </div>

    <!-- Mensagem de erro -->
    <div class="form-error" *ngIf="error()">{{ error() }}</div>

    <!-- Botões de ação -->
    <div class="form-actions">
      <button mat-stroked-button type="button" routerLink="/lista">Voltar</button>
      <button mat-raised-button color="primary" type="submit">Salvar</button>
    </div>
  </form>
</div>
```

## 📱 Responsividade

As classes são automaticamente responsivas:

- **Desktop (> 959px)**: Espaçamentos maiores, layout horizontal
- **Tablet (600px - 959px)**: Espaçamentos médios, layout adaptado
- **Mobile (< 599px)**: 
  - Espaçamentos reduzidos
  - Botões em coluna (full-width)
  - Títulos menores
  - Padding reduzido

## 🎨 Estilos Específicos

Se precisar de estilos específicos para um componente, adicione no arquivo `.scss` do componente:

```scss
// Estilos específicos do componente
// Os estilos globais de formulário estão em styles.scss

.meu-campo-especial {
  // Estilos específicos aqui
}
```

## ✅ Checklist para Novo Formulário

- [ ] Usar `.form-container` no container principal
- [ ] Usar `.form-title` no título
- [ ] Aplicar `.form-field-spacing` em todos os `mat-form-field`
- [ ] Envolver `mat-slide-toggle` em `.form-toggle-wrapper`
- [ ] Usar `.form-actions` para os botões
- [ ] Usar `.form-error` para mensagens de erro
- [ ] Usar `.form-section` para agrupar campos relacionados (opcional)
- [ ] Testar em mobile, tablet e desktop

## 🔗 Referências

- Estilos globais: `web/src/styles.scss`
- Exemplos de uso:
  - `web/src/app/components/cadastros/usuario/user-form.component.html`
  - `web/src/app/components/cadastros/perfil/perfil-form.component.html`

