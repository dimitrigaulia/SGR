export interface Perfil {
  id: number;
  nome: string;
  isAtivo: boolean;
  usuarioCriacao?: string;
  usuarioAtualizacao?: string;
  dataCriacao: Date;
  dataAtualizacao?: Date;
}

