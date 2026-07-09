// Catálogo de exibição das classes (PRD 3.5). As regras (bônus, pisos, títulos)
// são aplicadas pelo backend — aqui só nomes, emojis e descrições para a UI.

export interface ClasseInfo {
  id: string
  nome: string
  emoji: string
  primarias: string[] // ids de atributos
  lema: string
  progressao: string
}

export const CLASSES: ClasseInfo[] = [
  { id: 'guerreiro', nome: 'Guerreiro', emoji: '⚔️', primarias: ['forca', 'resistencia'], lema: 'Músculo sem mente é só uma arma sem punho.', progressao: 'Escudeiro → Senhor da Guerra' },
  { id: 'ranger', nome: 'Ranger', emoji: '🏹', primarias: ['resistencia', 'vitalidade'], lema: 'A trilha recompensa quem volta todo dia.', progressao: 'Batedor → Lorde das Trilhas' },
  { id: 'mago', nome: 'Mago', emoji: '🧙', primarias: ['inteligencia', 'conhecimento'], lema: 'A torre não sustenta um corpo em ruínas.', progressao: 'Aprendiz → Arcano' },
  { id: 'monge', nome: 'Monge', emoji: '🧘', primarias: ['espirito', 'disciplina'], lema: 'Silêncio por fora, constância por dentro.', progressao: 'Noviço → Iluminado' },
  { id: 'paladino', nome: 'Paladino', emoji: '🛡️', primarias: ['espirito', 'forca'], lema: 'Fé e ferro, temperados pelo descanso.', progressao: 'Escudeiro da Fé → Guardião Sagrado' },
  { id: 'mercador', nome: 'Mercador', emoji: '💰', primarias: ['financas', 'carisma'], lema: 'Ouro que não descansa vira burnout.', progressao: 'Ambulante → Rei Mercador' },
]

export const NOMES_ATRIBUTOS: Record<string, string> = {
  forca: '💪 Força', resistencia: '🏃 Resistência', vitalidade: '❤️ Vitalidade',
  recuperacao: '😴 Recuperação', inteligencia: '🧠 Inteligência', conhecimento: '📚 Conhecimento',
  espirito: '🙏 Espírito', financas: '💰 Finanças', carisma: '🤝 Carisma', disciplina: '⚡ Disciplina',
}
