export const onlyDigits = (value = '') => value.replace(/\D/g, '');
export const maskCpf = (value = '') => onlyDigits(value).replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d{1,2})$/, '$1-$2');
export const maskPhone = (value = '') => onlyDigits(value).replace(/(\d{2})(\d)/, '($1) $2').replace(/(\d{5})(\d{1,4})$/, '$1-$2');
