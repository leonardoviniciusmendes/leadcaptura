export type TipoLead = 'PessoaFisica' | 'Familia' | 'Mei' | 'Empresa';

export function digits(value: string): string {
  return value.replace(/\D/g, '');
}

export function maskPhone(value: string): string {
  const clean = digits(value).slice(0, 13);
  if (clean.startsWith('55') && clean.length > 4) {
    const area = clean.slice(2, 4);
    const number = clean.slice(4);
    if (number.length <= 4) return `+55 (${area}) ${number}`;
    const firstPartLength = number.length > 8 ? 5 : 4;
    return `+55 (${area}) ${number.slice(0, firstPartLength)}-${number.slice(firstPartLength)}`;
  }
  if (clean.length <= 2) return clean;
  if (clean.length <= 7) return `(${clean.slice(0, 2)}) ${clean.slice(2)}`;
  return `(${clean.slice(0, 2)}) ${clean.slice(2, 7)}-${clean.slice(7)}`;
}

export const brazilianMobileError = 'Informe um WhatsApp válido com DDD. Ex.: (21) 99999-9999.';

export function normalizeBrazilianMobile(value: string): string | null {
  if (!value || /[^\d\s()+-]/.test(value)) return null;

  let clean = digits(value);
  if (clean.length === 13 && clean.startsWith('55')) clean = clean.slice(2);
  return clean.length === 11 && clean[2] === '9' ? clean : null;
}

export function maskBrazilianMobile(value: string): string {
  if (/[^\d\s()+-]/.test(value)) return value;

  let clean = digits(value);
  const hasExplicitCountryCode = value.trimStart().startsWith('+55');
  if (hasExplicitCountryCode) {
    clean = clean.slice(2);
  } else if (clean.length === 13 && clean.startsWith('55')) {
    clean = clean.slice(2);
  } else if (clean.length > 11) {
    return value;
  }

  if (clean.length > 11) return value;

  if (clean.length <= 2) return clean;
  const ddd = clean.slice(0, 2);
  const number = clean.slice(2);
  if (number.length <= 5) return `(${ddd}) ${number}`;
  return `(${ddd}) ${number.slice(0, 5)}-${number.slice(5)}`;
}

export function maskCep(value: string): string {
  const clean = digits(value).slice(0, 8);
  return clean.length > 5 ? `${clean.slice(0, 5)}-${clean.slice(5)}` : clean;
}

export function maskCnpj(value: string): string {
  const clean = digits(value).slice(0, 14);
  return clean
    .replace(/^(\d{2})(\d)/, '$1.$2')
    .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1/$2')
    .replace(/(\d{4})(\d)/, '$1-$2');
}
