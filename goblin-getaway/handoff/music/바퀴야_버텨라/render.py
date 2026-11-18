import ctypes as ct
import ctypes.util
import math
import random
import numpy as np
from scipy.io.wavfile import write

RATE=48000
BEAT=.4
BARS=40
SAMPLES=round(BARS*4*BEAT*RATE)
rng=random.Random(9384)
sf=ct.CDLL(ctypes.util.find_library("fluidsynth"))
P=ct.c_void_p
I=ct.c_int
D=ct.c_double
C=ct.c_char_p
def call(name, result, args):
    f=getattr(sf,name)
    f.restype=result
    f.argtypes=args
    return f

settings=call('new_fluid_settings',P,[])()
setnum=call('fluid_settings_setnum',I,[P,C,D])
setint=call('fluid_settings_setint',I,[P,C,I])
for key,v in [('synth.sample-rate',RATE),('synth.gain',0.70),('synth.reverb.room-size',.32),('synth.reverb.damp',.62),('synth.reverb.width',.78),('synth.reverb.level',.24)]:
    setnum(settings,key.encode(),v)
setint(settings,b'synth.chorus.active',0)
synth=call('new_fluid_synth',P,[P])(settings)
font=call('fluid_synth_sfload',I,[P,C,I])(synth,b'/usr/share/sounds/sf2/TimGM6mb.sf2',1)
assert font>=0
program=call('fluid_synth_program_select',I,[P,I,I,I,I])
control=call('fluid_synth_cc',I,[P,I,I,I])
on=call('fluid_synth_noteon',I,[P,I,I,I])
off=call('fluid_synth_noteoff',I,[P,I,I])
render=call('fluid_synth_write_float',I,[P,I,P,I,I,P,I,I])
# Plucked bass, pizzicato, bassoon, clarinet, marimba, low brass, accordion, celesta.
for ch,(patch,vol,pan,space) in enumerate([(32,99,61,17),(45,91,44,36),(70,101,54,31),(71,87,77,35),(12,78,87,40),(58,85,58,30),(21,76,71,28),(8,64,92,40)]):
    assert program(synth,ch,font,0,patch)==0
    for key,v in [(7,vol),(10,pan),(91,space),(93,0)]:
        control(synth,ch,key,v)
assert program(synth,9,font,128,0)==0
for key,v in [(7,78),(10,64),(91,24)]:
    control(synth,9,key,v)
events=[]
def note(ch,b,t,key,d,v,feel=0):
    start=(b*4+t)*BEAT+feel+rng.uniform(-.003,.003)
    i=round(start*RATE)%SAMPLES
    vel=max(1,min(120,round(v+rng.uniform(-3,3))))
    events.append((i,1,ch,key,vel))
    events.append(((i+round(d*BEAT*RATE))%SAMPLES,0,ch,key,0))

harmony={
 'd':(38,45,[57,62,65]),'a':(33,40,[55,61,64]),'b':(34,41,[53,58,62]),'g':(43,38,[55,58,62]),
 'f':(41,36,[57,60,65]),'c':(36,43,[55,60,64]),'e':(40,46,[55,58,64]),'eb':(39,46,[55,58,63])}
progress=[
'd d g a d b e a','d f g a b g e a','b f c d g d e a','d d eb a g b e a','d f g a b g e a']
chords=' '.join(progress).split()
# Each tuple is the beat offset, pitch, articulation length and accent.
a=[
[(0,62,.40,86),(.75,65,.18,74),(1,69,.46,87),(1.75,68,.15,66),(2,69,.30,80),(2.5,65,.32,76),(3.25,62,.45,82)],
[(0,57,.45,79),(.75,62,.20,76),(1,65,.63,83),(2.25,64,.22,74),(2.75,62,.21,76),(3,57,.50,80)],
[(0,58,.38,82),(.75,62,.22,75),(1,67,.45,86),(1.75,66,.16,68),(2,67,.30,76),(2.5,62,.29,76),(3.25,58,.40,77)],
[(0,61,.36,78),(.75,64,.20,77),(1,67,.72,85),(2.5,65,.26,76),(3,64,.25,80),(3.5,61,.26,76)],
[(0,62,.42,86),(.75,65,.17,70),(1,69,.61,85),(2,74,.52,85),(2.75,72,.22,77),(3.25,69,.39,78)],
[(0,70,.64,83),(1,65,.35,78),(1.75,62,.21,76),(2,58,.59,79),(3,62,.34,74)],
[(0,64,.40,83),(.75,67,.22,72),(1,70,.37,80),(1.5,67,.25,73),(2.25,64,.30,77),(3,62,.37,74)],
[(0,61,.55,83),(1,64,.30,77),(1.5,67,.24,74),(2,69,.54,83),(3.5,61,.32,73)]]
bmel=[
[(0,70,.62,82),(1,69,.27,71),(1.5,65,.65,79),(2.75,62,.30,76),(3.25,65,.51,72)],
[(0,69,.78,82),(1.5,67,.25,71),(2,65,.68,78),(3,60,.36,76)],
[(0,67,.68,83),(1,64,.30,71),(1.5,62,.70,80),(2.75,60,.35,73),(3.5,64,.34,76)],
[(0,65,1.12,81),(1.5,69,.34,73),(2,74,.68,82),(3,69,.32,72)],
[(0,70,.60,81),(1,67,.36,73),(1.5,62,.70,80),(2.75,67,.32,76),(3.25,70,.50,75)],
[(0,69,.82,83),(1.5,65,.36,74),(2,62,.68,79),(3,57,.38,74)],
[(0,58,.47,78),(.75,62,.23,70),(1,64,.47,78),(1.75,67,.22,72),(2,70,.65,82),(3,67,.37,72)],
[(0,69,1.17,82),(1.5,67,.24,72),(2,64,.56,75),(3,61,.31,74),(3.5,64,.29,73)]]
sneak=[
[(.50,62,.20,79),(1,65,.26,77),(1.50,69,.47,82)],[(2,69,.24,82),(2.50,65,.21,72),(3,62,.45,79)],
[(.50,63,.20,75),(1,67,.26,77),(1.50,70,.47,82)],[(2,69,.24,80),(2.50,64,.22,73),(3,61,.45,81)],
[(.50,67,.21,76),(1,70,.29,78),(1.50,74,.48,82)],[(2,70,.25,80),(2.50,65,.22,73),(3,62,.49,81)],
[(0,64,.25,76),(.75,67,.23,76),(1.50,70,.29,78),(2.25,67,.25,74),(3,64,.35,79)],
[(0,61,.29,77),(.75,64,.23,75),(1.50,67,.29,79),(2.25,69,.25,76),(3,73,.36,81)]]
for bar,chord in enumerate(chords):
    section,k=divmod(bar,8)
    bass,fifth,tri=harmony[chord]
    low=section==3
    # Bass changes its pickup to lead a semitone into the next chord.
    bassline=[(0,bass,.63,84),(2,fifth,.48,75)]
    if not low and k%2==0: bassline += [(2.75,bass+12,.21,65),(3.5,fifth,.23,70)]
    if k%2==1 and not (low and k<5): bassline += [(3.5,harmony[chords[(bar+1)%BARS]][0]+(1 if k!=7 else -1),.26,76)]
    for t,key,d,v in bassline: note(0,bar,t,key,d,v,0.002)
    for j,t in enumerate([.5,1.5,2.5,3.5]):
        if low and j in [0,2]: continue
        if k==7 and j==3:continue
        notes=tri[1:] if j%2==0 else tri[:2]
        for n,key in enumerate(notes):note(1,bar,t,key,.22,70+(j%2)*5-(7 if low else 0),.005*n)
    # Dry woodblock gallop with softer answered taps.
    for t,key,vel in [(0,76,58),(.28,77,40),(.51,76,50),(1.51,77,46),(2,76,57),(2.29,77,41),(2.52,76,52),(3.53,77,46)]:
        if low and t in [.51,2.52,3.53]:continue
        note(9,bar,t,key,.11,vel+(3 if section==4 else 0),-.004)
    if not low or k>=6:
        for t in [0,2]: note(9,bar,t,36,.21,58+(4 if section==4 else 0))
        for t in [1,3]:note(9,bar,t,37 if section in [0,2] else 38,.15,56+(6 if t==3 else 0))
    if k in [3,7] and not (low and k==3):
        for t,vol in [(3.18,37),(3.34,39),(3.52,47),(3.72,55)]: note(9,bar,t,38 if k==7 else 50,.12,vol)
    line=(bmel if section==2 else sneak if low else a)[k]
    # On the reprise, the bassoon asks the question and clarinet finishes it.
    for t,key,d,vel in line:
        player=3 if section==2 or (section in [1,4] and k%2==1) else 2
        if low:player=4 if k%2==0 else 2
        if section==4 and k==7 and t>=2:continue
        note(player,bar,t,key,d,vel-(2 if low else 0),.009)
    if section==4 and k==7:
        for t,key,d,v in [(2,69,.28,80),(2.5,67,.24,76),(3,64,.24,78),(3.5,61,.25,75)]:
            note(3,bar,t,key,d,v)
    if section in [1,4] and k%2==0:
        for t,key in [(2.75,tri[1]+12),(3.50,tri[0]+12)]:note(4,bar,t,key,.22,61)
    if section in [2,4] and k in [1,3,5,7]:
        for t,key in [(0,tri[0]-12),(1.5,tri[1]-12)]: note(5,bar,t,key,.39,72 if k==7 else 65,.014)
    if section==2 and k%2==0:
        for key in tri[:2]:note(6,bar,1.50,key,.65,61,.016)
    if section in [1,4] and k in [2,6]:
        for t,key in [(1,tri[-1]+12),(2.5,tri[1]+12)]:note(7,bar,t,key,.27,63,.020)

events.sort()
# Feed the loop's final four measures before capturing, to carry room and note tails across the join.
pre=round(4*4*BEAT*RATE)
sequence=[(i-(SAMPLES-pre),a,ch,k,v) for i,a,ch,k,v in events if i>=SAMPLES-pre]
sequence += [(i+pre,a,ch,k,v) for i,a,ch,k,v in events]
audio=np.zeros((pre+SAMPLES,2),dtype=np.float32)
def synthesize(start,end):
    if end<=start:return
    address=audio.ctypes.data+start*8
    assert render(synth,end-start,P(address),0,2,P(address),1,2)==0
pos=0
for idx,act,ch,key,vel in sequence:
    synthesize(pos,idx)
    if act:on(synth,ch,key,vel)
    else:off(synth,ch,key)
    pos=idx
synthesize(pos,len(audio))
mix=audio[pre:]
print('duration',len(mix)/RATE,'peak',abs(mix).max(),'rms',np.sqrt(np.mean(mix*mix)))
write('mix.wav',RATE,mix)
