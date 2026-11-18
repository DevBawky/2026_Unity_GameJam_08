import ctypes as ct
import ctypes.util
import math
import random
import numpy as np
from scipy.io.wavfile import write

RATE=48000
BEAT=.1875
BARS=64
SAMPLES=round(BARS*6*BEAT*RATE)
rng=random.Random(771930)
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
# Double bass, plucked strings, clarinet, cup-muted brass, accordion, bassoon, xylophone, cello.
for ch,(patch,vol,pan,space) in enumerate([(32,101,61,16),(45,90,36,36),(71,88,75,32),(59,84,69,32),(21,77,54,27),(70,89,56,27),(13,75,80,32),(42,73,51,34)]):
    assert program(synth,ch,font,0,patch)==0
    for key,v in [(7,vol),(10,pan),(91,space),(93,0)]:control(synth,ch,key,v)
assert program(synth,9,font,128,0)==0
for key,v in [(7,80),(10,64),(91,19)]:control(synth,9,key,v)
events=[]
def note(ch,b,t,pitch,dur,vel,lag=0):
    if b==31:
        if t>=3:return
        dur=min(dur,2.88-t)
        if dur<=0:return
    i=round(((b*6+t)*BEAT+lag+rng.uniform(-.002,.002))*RATE)%SAMPLES
    v=max(1,min(127,int(vel+rng.uniform(-3,3))))
    events.extend([(i,1,ch,pitch,v),((i+round(dur*BEAT*RATE))%SAMPLES,0,ch,pitch,0)])
# G minor, the flat sixth, and a cheeky sharpened D dominant.
chords={
 'g':(43,38,[55,58,62]),'d':(38,45,[54,57,60]),'c':(36,43,[55,60,63]),
 'e':(39,46,[55,58,63]),'b':(34,41,[53,58,62]),'f':(41,36,[53,57,60]),
 'a':(33,39,[55,60,63]),'h':(35,42,[56,59,62])}
order=['ggcdbg ad'.replace(' ',''),'gecdbead','ebfbecad','gghdc ead'.replace(' ',''),'ggcdbg ad'.replace(' ',''),'ebfbecad','gchdc ead'.replace(' ',''),'gecdbead']
assert all(len(x)==8 for x in order)
# The theme starts with three descending notes, then an upward triplet imitation.
theme=[
 [(0,74,.65),(1,70,.65),(2,67,.72),(4,62,.55),(5,67,.65)],
 [(0,69,.48),(1,70,.48),(2,74,.49),(3,67,1.9)],
 [(0,75,.62),(1,72,.62),(2,67,.7),(4,63,.50),(5,67,.65)],
 [(0,69,.55),(1,72,.55),(2,74,.65),(3,66,2)],
 [(0,70,1.3),(2,65,.7),(3,62,.9),(4.5,65,.64)],
 [(0,67,.61),(1,65,.55),(2,63,.58),(3,62,1.65)],
 [(0,60,.70),(1.5,63,.63),(3,67,.62),(4.5,72,.68)],
 [(0,74,.54),(1,72,.55),(2,69,.65),(3,66,1.0),(5,69,.6)]]
chorus=[
 [(0,67,1.5),(2,70,.72),(3,75,1.22),(5,74,.58)],
 [(0,70,1.85),(3,65,.69),(4,67,.53),(5,70,.52)],
 [(0,72,1.47),(2,69,.62),(3,65,1.40),(5,67,.63)],
 [(0,70,2.25),(3,62,1.45)],
 [(0,67,.75),(1.3,70,.63),(3,75,1.6),(5,74,.53)],
 [(0,72,1.69),(2.5,67,.53),(3.5,63,.80),(5,60,.57)],
 [(0,63,.55),(1,64,.50),(2,67,.65),(3,72,1.61)],
 [(0,69,1.49),(2,66,.50),(3,62,.69),(4.5,66,.63)]]
sneak=[
 [(1,55,.57),(2,58,.57),(3,62,1.1)],[(0,67,.75),(4,62,.56),(5,58,.60)],
 [(1,56,.53),(2,59,.55),(3,62,.99)],[(0,66,.72),(4,62,.50),(5,57,.56)],
 [(1,55,.53),(2,60,.57),(3,63,.98)],[(0,67,.75),(4,63,.53),(5,58,.59)],
 [(0,57,.67),(1,60,.62),(2,63,.65),(3,67,1.02),(5,72,.57)],
 [(0,74,.62),(1,69,.59),(2,66,.54),(3,62,.95),(5,66,.59)]]
for bar in range(BARS):
    sec,idx=divmod(bar,8)
    root,fifth,chord=chords[order[sec][idx]]
    nextroot=chords[order[((bar+1)%BARS)//8][(bar+1)%8]][0]
    hushed=sec in (3,6)
    is_b=sec in (2,5)
    for t,k,du,v in [(0,root,1.55,83),(3,fifth,1.02,73)]:note(0,bar,t,k,du,v-(4 if hushed else 0))
    if idx%2 and (not hushed or idx>4):
        note(0,bar,5,nextroot+(1 if idx%4==1 else -1),.58,69)
    for t,j,u in ((1,0,.54),(2,1,.53),(4,0,.53),(5,1,.44)):
        if hushed and t in (1,4):continue
        if idx==7 and t==5:continue
        note(1,bar,t,chord[j],u,66 if j==0 else 62,.004)
        if not hushed and (j==1 or is_b):note(1,bar,t,chord[2],u,59,.009)
    # Hooves and axle are deliberately quieter than the winds.
    for t,key,v in [(0,76,59),(.68,77,42),(1.8,76,46),(3,77,56),(3.73,76,39),(4.85,77,48)]:
        if hushed and t in (.68,3.73):continue
        note(9,bar,t,key,.23,v+(2 if sec>3 else 0),.005)
    if not hushed or idx>=6:
        note(9,bar,0,36,.73,60);note(9,bar,3,37,.23,56)
        if sec in (1,4,7) and idx%2:note(9,bar,4.8,42,.15,43)
    if idx==7 and not hushed:
        for t,key,v in [(3.60,50,42),(4.20,50,48),(4.80,48,52),(5.4,45,58)]:note(9,bar,t,key,.31,v)
    line=(chorus if is_b else sneak if hushed else theme)[idx]
    lead=(4 if is_b and sec==2 else 2 if is_b else (5 if idx%2==0 else 6) if hushed else (3 if sec in (1,7) and idx%2==0 else 2))
    for j,(t,k,du) in enumerate(line):
        if sec in (1,7) and idx in (0,5) and j==len(line)-1:k+=5 if idx==0 else 3
        if sec==4 and idx==1 and j==3:k=70
        note(lead,bar,t,k,du,77+(6 if t in (0,3) else -2),.008)
    if sec in (1,4,7) and idx in (1,3,5):
        for t,k,v in [(3.8,chord[1]+12,61),(4.65,chord[0]+12,57)]:note(6,bar,t,k,.39,v)
    if is_b:
        note(7,bar,0,chord[0]-12,2.9,61)
        if idx%2==0: note(5,bar,4.15,chord[1],.65,65)
    if sec==7 and idx==7:
        note(3,bar,3,54,.65,63);note(3,bar,5,62,.58,66)
events.sort()
# Feed the loop's final four measures before capturing, to carry room and note tails across the join.
pre=round(8*6*BEAT*RATE)
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
