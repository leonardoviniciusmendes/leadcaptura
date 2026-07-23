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
