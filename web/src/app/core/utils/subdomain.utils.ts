/**
 * Utilitários para geração de subdomínio
 */

/**
 * Gera um subdomínio válido a partir de um nome
 * Remove acentos, converte para minúsculas, remove caracteres especiais
 * Exemplo: "Van Gogh Bar" -> "vangoghbar"
 */
export function generateSubdomain(nome: string): string {
  if (!nome) return '';

  // Normalizar e remover acentos
  let subdomain = nome
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '') // Remove diacríticos
    .toLowerCase()
    .trim();

  // Substituir espaços e caracteres especiais por nada (mantém apenas letras e números)
  subdomain = subdomain.replace(/[^a-z0-9]/g, '');

  // Remover números no início (subdomínios não podem começar com número)
  subdomain = subdomain.replace(/^\d+/, '');

  // Limitar tamanho (máximo 50 caracteres)
  if (subdomain.length > 50) {
    subdomain = subdomain.substring(0, 50);
  }

  return subdomain;
}

