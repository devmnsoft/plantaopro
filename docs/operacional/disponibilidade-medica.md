# Disponibilidade médica avançada

Médicos autenticados podem manter janelas de disponibilidade, indisponibilidades e preferências em endpoints próprios:

- `GET/POST/PUT/DELETE /api/medicos/me/disponibilidade`
- `GET/POST/DELETE /api/medicos/me/indisponibilidade`
- `GET/PUT /api/medicos/me/preferencias`
- `GET /api/coordenacao/medicos-disponiveis`

Regras implementadas: médico só altera o próprio cadastro; indisponibilidade bloqueia disponibilidade no mesmo período; coordenação consulta médicos do tenant; histórico e auditoria são registrados.
