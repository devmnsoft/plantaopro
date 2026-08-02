export async function moveItem(command, endpoint, antiforgeryToken) {
  const response = await fetch(endpoint, { method: 'POST', headers: { 'Content-Type':'application/json', 'RequestVerificationToken':antiforgeryToken }, body: JSON.stringify(command) });
  if (response.status === 409) throw new Error('Este item foi alterado em outra sessão. Atualize o quadro e tente novamente.');
  if (!response.ok) throw new Error('A movimentação não atende às regras atuais da operação.');
  return response.json();
}
export function enableAccessibleMove(button, destinations, execute) {
  button.addEventListener('click', () => destinations.focus()); destinations.addEventListener('change', () => execute(destinations.value));
}
