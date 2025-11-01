export interface Usuario {
  id: number;
  perfilId: number;
  isAtivo: boolean;
  nomeCompleto: string;
  email: string;
  pathImagem?: string;
  usuarioAtualizacao?: string;
  dataAtualizacao?: Date;
}

