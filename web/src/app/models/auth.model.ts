import { Usuario } from './usuario.model';
import { Perfil } from './perfil.model';

export interface LoginRequest {
  email: string;
  senha: string;
}

export interface LoginResponse {
  token: string;
  usuario: Usuario;
  perfil: Perfil;
}

