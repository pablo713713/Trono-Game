# Simula 2000 partidas por estrategia con las reglas de GameState.cs (versión 2).
# Uso, desde la raíz del proyecto:  python Tools/simular_balance.py
#
# Reglas que replica (si cambian en Reglas.cs o GameState.cs, cambiarlas aquí también):
# - Ánimo inicial 50 por grupo, entre 0 y 100. Pierdes cuando un grupo llega a 0.
# - Peticiones al azar de todo el banco, sin repetirse; si alcanza, una de cada grupo por día.
# - Mensajeros por día: 4, 6, 8, 10, 12. El ánimo se conserva de un día a otro.
# - El oro no cambia.
#
# Objetivos de balance: sí a todo y no a todo pierden; al azar casi siempre pierde;
# jugar pensando llega al Duque.
import json, random
from collections import Counter

DIAS = [4, 6, 8, 10, 12]
GRUPOS = ['campesinos', 'soldados', 'iglesia']
peticiones = json.load(open('Assets/Data/requests.json', encoding='utf-8'))['peticiones']


def efectos(p, si):
    e = p['si'] if si else p['no']
    return [e.get(g, 0) for g in GRUPOS]


def jugar(estrategia, semilla):
    r = random.Random(semilla)
    animo = [50, 50, 50]
    banco = list(peticiones)
    for dia, n in enumerate(DIAS, 1):
        r.shuffle(banco)
        hoy = []
        for g in GRUPOS:
            for p in banco:
                if p['grupo'] == g:
                    hoy.append(p)
                    banco.remove(p)
                    break
        while len(hoy) < n and banco:
            hoy.append(banco.pop(0))
        r.shuffle(hoy)
        for p in hoy:
            e = efectos(p, estrategia(p, animo, r))
            animo = [max(0, min(100, animo[i] + e[i])) for i in range(3)]
            for i in range(3):
                if animo[i] <= 0:
                    return 'cae_' + GRUPOS[i], dia
    return 'duque', 6


def minimo_tras(p, animo, si):
    e = efectos(p, si)
    return min(max(0, min(100, animo[i] + e[i])) for i in range(3))


def pensada(p, animo, r):
    # Mira los balcones: elige la respuesta que deja mejor al grupo más enojado.
    s, n = minimo_tras(p, animo, True), minimo_tras(p, animo, False)
    if s != n:
        return s > n
    return sum(efectos(p, True)) >= sum(efectos(p, False))


ESTRATEGIAS = {
    'todo_si': lambda p, a, r: True,
    'todo_no': lambda p, a, r: False,
    'al_azar': lambda p, a, r: r.random() < 0.5,
    'pensada': pensada,
    'jugador_real': lambda p, a, r: pensada(p, a, r) if r.random() < 0.8 else r.random() < 0.5,
}

print(f"{len(peticiones)} peticiones; una partida usa {sum(DIAS)}")
for nombre, estrategia in ESTRATEGIAS.items():
    resultados, dias = Counter(), Counter()
    for s in range(2000):
        res, dia = jugar(estrategia, s)
        resultados[res] += 1
        if res != 'duque':
            dias[dia] += 1
    print(f"{nombre:13s} llega al Duque {resultados['duque'] / 20:5.1f}% | derrotas por día {dict(sorted(dias.items()))}")
