/**
 * UtilitÃ¡rios para geraÃ§Ã£o de subdomÃ­nio
 */

/**
 * Gera um subdomÃ­nio vÃ¡lido a partir de um nome
 * Remove acentos, converte para minÃºsculas, remove caracteres especiais
 * Exemplo: "Van Gogh Bar" -> "vangoghbar"
 */
export function generateSubdomain(nome: string): string {
  if (!nome) return '';

  // Normalizar e remover acentos
  let subdomain = nome
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '') // Remove diacrÃ­ticos
    .toLowerCase()
    .trim();

  // Substituir espaÃ§os e caracteres especiais por nada (mantÃ©m apenas letras e nÃºmeros)
  subdomain = subdomain.replace(/[^a-z0-9]/g, '');

  // Remover nÃºmeros no inÃ­cio (subdomÃ­nios nÃ£o podem comeÃ§ar com nÃºmero)
  subdomain = subdomain.replace(/^\d+/, '');

  // Limitar tamanho (mÃ¡ximo 50 caracteres)
  if (subdomain.length > 50) {
    subdomain = subdomain.substring(0, 50);
  }

  return subdomain;
}

