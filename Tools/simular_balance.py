# Simula 2000 partidas por estrategia con las reglas de GameState.cs.
# Uso, desde la raiz del proyecto: python Tools/simular_balance.py
# Si cambian Reglas.cs (impuestos, calma, exigencia), actualizar los mismos numeros aqui.
import json, random, sys
from collections import Counter
L=json.load(open('Assets/Data/requests.json'))['peticiones']
G=['campesinos','soldados','iglesia']
def jugar(estrategia, seed):
    rnd=random.Random(seed); an={g:50 for g in G}; oro=100; ex={g:0 for g in G}
    for dia in range(1,6):
        hoy=[p for p in L if p['dia']==dia]; rnd.shuffle(hoy)
        for p in hoy:
            si=estrategia(p,an,oro,rnd)
            e=p['si'] if si else p['no']; co=e['oro']
            if co<0: co=round(co*(1+0.2*ex[p['grupo']]))
            for g in G: an[g]=max(0,min(100,an[g]+e[g]))
            oro=max(0,oro+co)
            ex[p['grupo']]=ex[p['grupo']]+1 if si else max(0,ex[p['grupo']]-1)
            for g in G:
                if an[g]<=0: return f'rebelion_{g}',dia
            if oro<=0: return 'bancarrota',dia
        for g in G:
            if an[g]<50: an[g]=min(100,an[g]+5)
        oro+=25
    return 'duque',(an,oro)
E={
 'todo_si':lambda p,a,o,r:True,
 'todo_no':lambda p,a,o,r:False,
 'solo_justas':lambda p,a,o,r:not p['trampa'],
 'al_azar':lambda p,a,o,r:r.random()<0.5,
 'pensada':lambda p,a,o,r:((not p['trampa']) and o+p['si']['oro']>15) or (a[p['grupo']]<20 and o+p['si']['oro']>40),
 'jugador_real':lambda p,a,o,r:(((not p['trampa']) and o+p['si']['oro']>15) or (a[p['grupo']]<20 and o+p['si']['oro']>40)) if r.random()<0.8 else r.random()<0.5,
}
for n,f in E.items():
    c=Counter(); ej=None
    for s in range(2000):
        res,info=jugar(f,s); c[res if res!='duque' else 'llega_al_duque']+=1
        if res!='duque': c[f'  dia_{info}']+=1
        else: ej=info
    print(n, dict(c.most_common()), 'ejemplo final:',ej if ej else '')
