from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import google.generativeai as genai
import re

# --- Configuration API Gemini ---
GOOGLE_API_KEY = "AIzaSyDAS9yKZDgJGn_vcAyficnAzA3JsMMc5gg"
genai.configure(api_key=GOOGLE_API_KEY)
model = genai.GenerativeModel("gemini-1.5-flash")

app = FastAPI(title="Évaluation Automatique Multilingue")

# --- Modèles d’entrée et sortie ---
class EvalRequest(BaseModel):
    niveau: str
    question: str
    reponse_ref: str
    reponse_eleve: str

class EvalResponse(BaseModel):
    note: float
    feedback: str

# --- Fonction utilitaire : appel Gemini ---
def ask_gemini(prompt, as_number=False):
    response = model.generate_content(prompt, generation_config={"temperature": 0})
    text = response.text.strip()
    if as_number:
        match = re.search(r"\b\d+(\.\d+)?\b", text)
        if match:
            return float(match.group())
        raise ValueError(f"Aucune note trouvée : {text}")
    return text

# === 🚀 ÉVALUATION ARABE ===
def eval_grammaire_arabe(rep, niveau):
    return ask_gemini(f"""أنت أستاذ متخصص في النحو العربي، وتقيم جملة لطالب في المستوى {niveau}.
النص: "{rep}"
قيّم جودة التركيب النحوي بدقة، على مقياس من 10، مع أخذ القواعد النحوية المناسبة للمستوى بعين الاعتبار.
أعط رقماً فقط (بدون شرح).""", as_number=True)

def eval_orthographe_arabe(rep, niveau):
    return ask_gemini(f"""أنت خبير في الإملاء باللغة العربية. هذه إجابة طالب في المستوى {niveau}:
"{rep}"
قيّم الإملاء فقط على 10، مع مراعاة الأخطاء الإملائية البسيطة والشائعة حسب المستوى. أجب برقم فقط.""", as_number=True)

def eval_validite_arabe(rep, ref, niveau):
    return ask_gemini(f"""السياق: تقييم مدى صحة إجابة طالب في المستوى {niveau}.
الإجابة المرجعية: "{ref}"
إجابة الطالب: "{rep}"
قيّم مدى تطابق المعنى والدقة المفهومية على 10. أجب فقط برقم.""", as_number=True)

def eval_contenu_arabe(rep, ref):
    return ask_gemini(f"""قم بتقييم جودة المحتوى التعليمي في هذه الإجابة.
الإجابة المرجعية: "{ref}"
إجابة الطالب: "{rep}"
قيم مدى شمولية وتفصيل المحتوى على 10. أجب فقط برقم.""", as_number=True)

def eval_mots_cles_arabe(rep, ref):
    return ask_gemini(f"""تقييم عدد وأهمية الكلمات المفتاحية الواردة في إجابة الطالب مقارنة بالإجابة المرجعية.
المرجعية: "{ref}"
إجابة الطالب: "{rep}"
قيّم التغطية الاصطلاحية من 10. أجب فقط برقم.""", as_number=True)

def generate_feedback_arabe(rep, niveau):
    return ask_gemini(f"""أنت معلم مشجع ولديك خبرة في التعامل مع طلاب في المستوى {niveau}.
إجابة الطالب: "{rep}"
اكتب ملاحظة تحفيزية قصيرة تُشجّع الطالب على تحسين مستواه دون إحباطه.""")

@app.post("/eval", response_model=EvalResponse)
def evaluer_arabe(req: EvalRequest):
    try:
        niveau = req.niveau.strip()
        notes = {
            "grammaire": eval_grammaire_arabe(req.reponse_eleve, niveau),
            "orthographe": eval_orthographe_arabe(req.reponse_eleve, niveau),
            "validite": eval_validite_arabe(req.reponse_eleve, req.reponse_ref, niveau),
            "contenu": eval_contenu_arabe(req.reponse_eleve, req.reponse_ref),
            "mots_cles": eval_mots_cles_arabe(req.reponse_eleve, req.reponse_ref)
        }
        note_finale = round(sum(notes.values()) / len(notes), 1)
        feedback = generate_feedback_arabe(req.reponse_eleve, niveau)
        return {"note": note_finale, "feedback": feedback}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

# === 🇫🇷 ÉVALUATION FRANÇAISE ===
def eval_grammaire_fr(rep, niveau):
    return ask_gemini(f"""Tu es un enseignant expérimenté en grammaire française. Voici une phrase d’un élève de niveau {niveau} :
"{rep}"
Évalue uniquement la **structure grammaticale** (accords, conjugaison, syntaxe) sur 10. Réponds uniquement par un nombre.""", as_number=True)

def eval_orthographe_fr(rep, niveau):
    return ask_gemini(f"""Tu es un correcteur expert en orthographe pour le niveau {niveau}.
Voici la phrase d’un élève : "{rep}"
Note **l’orthographe des mots** (y compris les accents et homophones) sur 10. Réponds uniquement avec un nombre.""", as_number=True)

def eval_validite_fr(rep, ref, niveau):
    return ask_gemini(f"""Contexte : un élève de niveau {niveau} a répondu à une question.
Réponse attendue : "{ref}"
Réponse de l’élève : "{rep}"
Évalue la **cohérence sémantique** et la pertinence par rapport à la réponse attendue, sur 10. Réponds uniquement avec un chiffre.""", as_number=True)

def eval_contenu_fr(rep, ref):
    return ask_gemini(f"""Tu es professeur de sciences. Compare cette réponse à la référence suivante :
Référence : "{ref}"
Réponse élève : "{rep}"
Note la **richesse du contenu explicatif** sur 10. Réponds seulement par un nombre.""", as_number=True)

def eval_mots_cles_fr(rep, ref):
    return ask_gemini(f"""Tu évalues la présence des mots-clés importants dans une réponse d’élève.
Référence : "{ref}"
Réponse : "{rep}"
Note la **présence et l’usage correct** des mots-clés essentiels sur 10. Réponds uniquement avec un chiffre.""", as_number=True)

def generate_feedback_fr(rep, niveau):
    return ask_gemini(f"""Tu es un enseignant encourageant et bienveillant face à un élève de niveau {niveau}.
Voici sa réponse : "{rep}"
Donne un **court commentaire motivant** pour féliciter ou inciter l’élève à progresser.""")

@app.post("/eval-fr", response_model=EvalResponse)
def evaluer_francais(req: EvalRequest):
    try:
        niveau = req.niveau.strip()
        notes = {
            "grammaire": eval_grammaire_fr(req.reponse_eleve, niveau),
            "orthographe": eval_orthographe_fr(req.reponse_eleve, niveau),
            "validite": eval_validite_fr(req.reponse_eleve, req.reponse_ref, niveau),
            "contenu": eval_contenu_fr(req.reponse_eleve, req.reponse_ref),
            "mots_cles": eval_mots_cles_fr(req.reponse_eleve, req.reponse_ref)
        }
        note_finale = round(sum(notes.values()) / len(notes), 1)
        feedback = generate_feedback_fr(req.reponse_eleve, niveau)
        return {"note": note_finale, "feedback": feedback}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))