/**
 * Utilitários para máscaras de CPF/CNPJ
 */

/**
 * Aplica máscara de CPF (000.000.000-00)
 */
export function applyCpfMask(value: string): string {
  const numbers = value.replace(/\D/g, '');
  if (numbers.length <= 3) return numbers;
  if (numbers.length <= 6) return `${numbers.slice(0, 3)}.${numbers.slice(3)}`;
  if (numbers.length <= 9) return `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6)}`;
  return `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6, 9)}-${numbers.slice(9, 11)}`;
}

/**
 * Aplica máscara de CNPJ (00.000.000/0000-00)
 */
export function applyCnpjMask(value: string): string {
  const numbers = value.replace(/\D/g, '');
  if (numbers.length <= 2) return numbers;
  if (numbers.length <= 5) return `${numbers.slice(0, 2)}.${numbers.slice(2)}`;
  if (numbers.length <= 8) return `${numbers.slice(0, 2)}.${numbers.slice(2, 5)}.${numbers.slice(5)}`;
  if (numbers.length <= 12) return `${numbers.slice(0, 2)}.${numbers.slice(2, 5)}.${numbers.slice(5, 8)}/${numbers.slice(8)}`;
  return `${numbers.slice(0, 2)}.${numbers.slice(2, 5)}.${numbers.slice(5, 8)}/${numbers.slice(8, 12)}-${numbers.slice(12, 14)}`;
}

/**
 * Remove máscara de CPF/CNPJ
 */
export function removeMask(value: string): string {
  return value.replace(/\D/g, '');
}

/**
 * Aplica máscara dinâmica baseada no tipo de pessoa
 * @param value Valor a ser mascarado
 * @param tipoPessoaId 1 = CPF, 2 = CNPJ
 */
export function applyCpfCnpjMask(value: string, tipoPessoaId: number | null): string {
  if (!tipoPessoaId) return value;
  if (tipoPessoaId === 1) return applyCpfMask(value);
  if (tipoPessoaId === 2) return applyCnpjMask(value);
  return value;
}

