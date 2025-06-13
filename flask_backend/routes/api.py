from flask import Blueprint, jsonify, request
from utils.db import db
from bson import ObjectId
import random

bp = Blueprint("api", __name__, url_prefix="/api")


# 🔹 Get all students 
@bp.route("/students", methods=["GET"])
def get_all_students():
    students_cursor = db.students.find({}, {"_id": 0, "password_hash": 0})
    students = list(students_cursor)
    return jsonify(students), 200

# 🔹 Get a student by index
@bp.route("/student-by-index/<int:index>", methods=["GET"])
def get_student_by_index(index):
    students = list(db.students.find({}, {"_id": 0, "password_hash": 0}))
    
    if index < 0 or index >= len(students):
        return jsonify({"error": "Index out of range"}), 404

    return jsonify(students[index]), 200


# 🔹 Update Student Level 
@bp.route("/students/update-level", methods=["POST"])
def update_student_level():
    data = request.get_json()
    username = data.get("username")
    new_level = data.get("level")

    if not username or not new_level:
        return jsonify({"error": "Missing data"}), 400

    result = db.students.update_one(
        {"username": username},
        {"$set": {"level": new_level}}
    )

    if result.matched_count == 0:
        return jsonify({"error": "Student not found"}), 404

    return jsonify({"message": "Level updated"}), 200


# 🔹 Delete Student
@bp.route("/students/delete/<string:username>", methods=["DELETE"])
def delete_student(username):
    result = db.students.delete_one({"username": username})
    
    if result.deleted_count == 0:
        return jsonify({"error": "Student not found"}), 404

    return jsonify({"message": "Student deleted"}), 200


# 🔹 Get all texts with their open questions
@bp.route("/all", methods=["GET"])
def get_all_question_sets():
    texts = db.texts.find()
    output = []

    for text_doc in texts:
        text_id = text_doc["_id"]
        text_content = text_doc.get("text_content", "")

        questions = list(db.questions.find({"text_id": text_id}))
        formatted_questions = []
        for q in questions:
            formatted_questions.append({
                "question": q.get("question"),
                "reference_answer": q.get("reference_answer"),
                "type": q.get("type"),
            })

        output.append({
            "id": str(text_id),
            "text": text_content,
            "questions": formatted_questions
        })

    return jsonify(output), 200

# 🔹 Get one random text with open questions (used by Unity)
@bp.route("/random-text-open-questions", methods=["GET"])
def get_random_text_with_open_questions():
    texts = list(db.texts.find())
    if not texts:
        return jsonify({"error": "No texts found"}), 404

    selected_text = random.choice(texts)
    text_id = selected_text["_id"]

    questions = list(db.questions.find({"text_id": text_id}))
    formatted_questions = [
        {
            "question": q.get("question"),
            "reference_answer": q.get("reference_answer"),
            "type": q.get("type"),
        } for q in questions
    ]

    return jsonify({
        "text": {
            "_id": str(text_id),
            "text_content": selected_text.get("text_content", "")
        },
        "questions": formatted_questions
    }), 200

@bp.route("/text-by-index/<int:index>", methods=["GET"])
def get_text_by_index(index):
    texts = list(db.texts.find())
    if index < 0 or index >= len(texts):
        return jsonify({"error": "Index out of range"}), 404

    text_doc = texts[index]
    text_id = text_doc["_id"]
    text_content = text_doc.get("text_content", "")

    questions_cursor = db.questions.find({"text_id": text_id})
    questions = [{
        "question": q.get("question"),
        "reference_answer": q.get("reference_answer"),
        "type": q.get("type")
    } for q in questions_cursor]

    return jsonify({
        "text": {
            "id": str(text_id),
            "text_content": text_content
        },
        "questions": questions
    }), 200

# 🔹 Get all closed (multiple-choice) questions
@bp.route("/closed-questions", methods=["GET"])
def get_closed_questions():
    questions = list(db.closed_questions.find())
    output = [
        {
            "question": q.get("question"),
            "answers": q.get("answers"),  # expects a list of strings
            "correctAnswerIndex": q.get("correctAnswerIndex")
        }
        for q in questions
    ]
    return jsonify(output), 200

# 🔹 Save user answers 
@bp.route("/user-answers", methods=["POST"])
def save_user_answers():
    data = request.get_json()
    if not data or "answers" not in data:
        return jsonify({"error": "Invalid data"}), 400

    db.answers.insert_one(data)
    return jsonify({"message": "Answers saved successfully"}), 201

# 🔹 Save reading recordings in base 64 
@bp.route("/reading-recordings", methods=["POST"])
def save_reading_recording():
    data = request.get_json()
    if not data or "audio_base64" not in data:
        return jsonify({"error": "Missing audio_base64 field"}), 400

    db.reading_recordings.insert_one(data)
    return jsonify({"message": "Recording saved"}), 201

# 🔹 Save open question answers (structured by student + text)
@bp.route("/open-question-answers", methods=["POST"])
def save_open_question_answers():
    data = request.get_json()

    student_id = data.get("student_id")
    text_id = data.get("text_id")
    answers = data.get("answers")
    final_score = data.get("final_score")

    if not student_id or not text_id or not isinstance(answers, list):
        return jsonify({"error": "Missing or invalid data"}), 400

    doc = {
        "student_id": student_id,
        "text_id": text_id,
        "answers": answers,
        "final_score": final_score
    }

    db.open_answers.insert_one(doc)
    return jsonify({"message": "Open question answers saved"}), 201

# 🔹 Get all unreviewed open question answers
@bp.route("/open-question-answers", methods=["GET"])
def get_open_question_answers():
    results = []
    cursor = db.open_answers.find()

    for doc in cursor:
        results.append({
            "_id": str(doc["_id"]),
            "student_id": doc.get("student_id"),
            "text_id": doc.get("text_id"),
            "final_score": doc.get("final_score"),
            "answers": doc.get("answers", [])
        })

    return jsonify(results), 200

# 🔹 Get unreviewed open question answers by student id
@bp.route("/open-question-answers/<string:student_id>", methods=["GET"])
def get_open_answers_by_student(student_id):
    cursor = db.open_answers.find({"student_id": student_id})

    results = []
    for doc in cursor:
        results.append({
            "_id": str(doc["_id"]),
            "student_id": doc.get("student_id"),
            "text_id": doc.get("text_id"),
            "final_score": doc.get("final_score"),
            "answers": doc.get("answers", [])
        })

    return jsonify(results), 200

# 🔹 Get unreviewed open question answers by text id
@bp.route("/open-question-answers/<string:text_id>", methods=["GET"])
def get_open_answers_by_text(text_id):
    cursor = db.open_answers.find({"text_id": text_id})

    results = []
    for doc in cursor:
        results.append({
            "_id": str(doc["_id"]),
            "student_id": doc.get("student_id"),
            "text_id": doc.get("text_id"),
            "final_score": doc.get("final_score"),
            "answers": doc.get("answers", [])
        })

    return jsonify(results), 200
