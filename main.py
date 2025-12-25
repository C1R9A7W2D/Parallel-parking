import time
import pygame
import math
import sys
import json
from typing import List, Tuple, Dict, Optional, Any, TypedDict
from enum import Enum
from dataclasses import dataclass
import random

# Константы
WIDTH, HEIGHT = 1400, 700
FPS = 60
CAR_LENGTH = 80
CAR_WIDTH = 40
SENSOR_RANGE = 400


class ParkingPhase(Enum):
    SEARCHING = "searching"
    APPROACH = "approach"
    POSITIONING = "positioning"
    PREPARE_REVERSE = "prepare_reverse"
    REVERSE_RIGHT = "reverse_right"
    REVERSE_LEFT = "reverse_left"
    FINAL_ADJUST = "final_adjust"
    PARKED = "parked"
    ABORTED = "aborted"


class Direction(Enum):
    FORWARD = "forward"
    BACKWARD = "backward"


class RulePriority(Enum):
    HIGH = 3
    MEDIUM = 2
    LOW = 1


@dataclass
class ParkingSpot:
    x: float
    y: float
    width: float = 100
    length: float = 120
    occupied: bool = False
    suitable: bool = True


@dataclass
class ParkingConfig:
    turn_radius_ratio: float = 2.5
    sensor_range: float = SENSOR_RANGE
    max_speed: float = 2.5
    max_reverse_speed: float = 1.5
    preferred_direction: Direction = Direction.FORWARD
    safety_margin: float = 30.0
    steering_sensitivity: float = 0.3


@dataclass
class Rule:
    name: str
    priority: RulePriority
    conditions: List[str]
    actions: Dict[str, Any]
    description: str

    def evaluate(self, context: Dict[str, Any]) -> bool:
        """Оценка условий правила"""
        try:
            for condition in self.conditions:
                # Простая замена переменных
                eval_condition = condition

                # Заменяем переменные из контекста
                for key, value in context.items():
                    if key in eval_condition and isinstance(value, (int, float)):
                        # Для числовых значений
                        eval_condition = eval_condition.replace(key, str(value))
                    elif key in eval_condition and isinstance(value, str):
                        # Для строковых значений (фазы)
                        if f"{key} ==" in eval_condition:
                            eval_condition = eval_condition.replace(f"{key} ==", f"'{value}' ==")
                        elif f"{key} !=" in eval_condition:
                            eval_condition = eval_condition.replace(f"{key} !=", f"'{value}' !=")

                # Также заменяем константы
                if "CAR_LENGTH" in eval_condition:
                    eval_condition = eval_condition.replace("CAR_LENGTH", str(CAR_LENGTH))
                if "CAR_WIDTH" in eval_condition:
                    eval_condition = eval_condition.replace("CAR_WIDTH", str(CAR_WIDTH))

                # Выполняем условие
                if not eval(eval_condition, {"math": math}, context):
                    return False
            return True
        except Exception as e:
            # print(f"Ошибка в правиле '{self.name}': {e}, условие: {condition}")
            return False
        

class DecisionInfo(TypedDict):
    throttle: float
    steering: float
    reasoning: str
    emergency: bool
    rule_applied: str
    rule_description: str


class KnowledgeBase:
    """База знаний с правилами парковки"""

    def __init__(self, config: ParkingConfig):
        self.config = config
        self.rules = self._build_rules()
        self.facts = {}
        self.initialized = False

    def _build_rules(self) -> Dict[str, Rule]:
        """Создание базы правил для парковки"""
        rules = {}

        # === ПРАВИЛА ИНИЦИАЛИЗАЦИИ ===

        # Правило 1: Начало движения при старте
        rules["initial_start"] = Rule(
            name="Начало движения",
            priority=RulePriority.HIGH,
            conditions=[
                "phase == 'searching'",
                "car_x < 300"  # Только в начале пути
            ],
            actions={
                "throttle": "config.max_speed * 0.7",
                "steering": 0.0,
                "reasoning": "Начало движения по дороге",
                "emergency": False
            },
            description="Начальное движение при старте системы"
        )

        # === ПРАВИЛА БЕЗОПАСНОСТИ ===

        # Правило 2: Экстренная остановка
        rules["emergency_stop"] = Rule(
            name="Экстренная остановка",
            priority=RulePriority.HIGH,
            conditions=[
                "front_sensor < 30",
                "throttle > 0"
            ],
            actions={
                "throttle": 0.0,
                "steering": 0.0,
                "reasoning": "Экстренная остановка: препятствие впереди",
                "emergency": True
            },
            description="Остановка при опасном сближении"
        )

        # Правило 3: Замедление у препятствия
        rules["slow_down"] = Rule(
            name="Замедление",
            priority=RulePriority.HIGH,
            conditions=[
                "front_sensor < 100",
                "throttle > 0"
            ],
            actions={
                "throttle": "throttle * 0.5",
                "steering": 0.0,
                "reasoning": "Замедление: препятствие впереди",
                "emergency": False
            },
            description="Замедление при приближении к препятствию"
        )

        # === ПРАВИЛА ПОИСКА МЕСТА ===

        # Правило 4: Прямое движение при поиске
        rules["straight_search"] = Rule(
            name="Прямой поиск",
            priority=RulePriority.MEDIUM,
            conditions=[
                "phase == 'searching'",
                "selected_spot is None"
            ],
            actions={
                "throttle": "config.max_speed * 0.7",
                "steering": 0.0,
                "reasoning": "Поиск свободного парковочного места",
                "emergency": False
            },
            description="Прямое движение для поиска места"
        )

        # Правило 5: Коррекция центра
        rules["center_correction"] = Rule(
            name="Коррекция центра",
            priority=RulePriority.MEDIUM,
            conditions=[
                "phase == 'searching'",
                "abs(left_sensor - right_sensor) > 50"
            ],
            actions={
                "throttle": "config.max_speed * 0.7",
                "steering": "(right_sensor - left_sensor) * 0.01",
                "reasoning": "Коррекция для движения по центру",
                "emergency": False
            },
            description="Коррекция положения на дороге"
        )

        # Правило 6: Место найдено
        rules["spot_found"] = Rule(
            name="Место найдено",
            priority=RulePriority.MEDIUM,
            conditions=[
                "phase == 'searching'",
                "selected_spot is not None",
                "car_x < selected_spot.x - 100"
            ],
            actions={
                "throttle": "config.max_speed * 0.6",
                "steering": 0.0,
                "reasoning": "Найдено место, продолжаем движение",
                "emergency": False
            },
            description="Движение к найденному месту"
        )

        # === ПРАВИЛА ПОДЪЕЗДА ===

        # Правило 7: Подъезд к месту
        rules["approach_spot"] = Rule(
            name="Подъезд к месту",
            priority=RulePriority.MEDIUM,
            conditions=[
                "phase == 'approach'",
                "selected_spot is not None",
                "car_x < selected_spot.x + CAR_LENGTH * 1.5"
            ],
            actions={
                "throttle": "config.max_speed * 0.5",
                "steering": "-(car_y - (selected_spot.y - CAR_WIDTH * 1.5)) * 0.008",
                "reasoning": "Подъезд к позиции для парковки",
                "emergency": False
            },
            description="Подъезд к позиции перед парковкой"
        )

        # Правило 8: Остановка для маневра
        rules["stop_for_maneuver"] = Rule(
            name="Остановка для маневра",
            priority=RulePriority.MEDIUM,
            conditions=[
                "phase == 'approach'",
                "selected_spot is not None",
                "car_x >= selected_spot.x + CAR_LENGTH * 1.5 - 40",
                "car_x <= selected_spot.x + CAR_LENGTH * 1.5 + 40"
            ],
            actions={
                "throttle": 0.0,
                "steering": 0.0,
                "reasoning": "Позиция достигнута, подготовка к парковке",
                "emergency": False
            },
            description="Остановка на позиции для парковки"
        )

        # === ПРАВИЛА ПОЗИЦИОНИРОВАНИЯ ===

        # Правило 9: Выравнивание
        rules["align_parallel"] = Rule(
            name="Выравнивание",
            priority=RulePriority.MEDIUM,
            conditions=[
                "phase == 'positioning'",
                "selected_spot is not None",
                "abs(car_angle) > 2 or abs(car_y - (selected_spot.y - CAR_WIDTH * 1.5)) > 15"
            ],
            actions={
                "throttle": "config.max_speed * 0.3",
                "steering": "-car_angle * 0.15 - (car_y - (selected_spot.y - CAR_WIDTH * 1.5)) * 0.01",
                "reasoning": "Выравнивание параллельно парковке",
                "emergency": False
            },
            description="Выравнивание автомобиля"
        )

        # Правило 10: Готовность к реверсу
        rules["ready_for_reverse"] = Rule(
            name="Готовность к реверсу",
            priority=RulePriority.MEDIUM,
            conditions=[
                "phase == 'positioning'",
                "selected_spot is not None",
                "abs(car_angle) < 3",
                "abs(car_y - (selected_spot.y - CAR_WIDTH * 1.5)) < 15",
                "front_right_sensor < 120"
            ],
            actions={
                "throttle": 0.0,
                "steering": 0.0,
                "reasoning": "Готовность к заднему маневру",
                "emergency": False
            },
            description="Автомобиль готов к заднему маневру"
        )

        # === ПРАВИЛА ЗАДНЕГО МАНЕВРА ===

        # Правило 11: Начало заднего маневра
        rules["start_reverse"] = Rule(
            name="Начало заднего маневра",
            priority=RulePriority.HIGH,
            conditions=[
                "phase == 'reverse_right'",
                "car_angle > -35"
            ],
            actions={
                "throttle": "-config.max_reverse_speed * 0.3",
                "steering": "20",
                "reasoning": "Задний маневр с поворотом",
                "emergency": False
            },
            description="Начало заднего маневра"
        )

        # Правило 12: Переход к выравниванию
        rules["transition_to_align"] = Rule(
            name="Переход к выравниванию",
            priority=RulePriority.HIGH,
            conditions=[
                "phase == 'reverse_right'",
                "car_angle <= -30"
            ],
            actions={
                "throttle": "-config.max_reverse_speed * 0.2",
                "steering": "0",
                "reasoning": "Переход к выравниванию",
                "emergency": False
            },
            description="Переход к фазе выравнивания"
        )

        # Правило 13: Выравнивание задним ходом
        rules["reverse_align"] = Rule(
            name="Выравнивание задним ходом",
            priority=RulePriority.MEDIUM,
            conditions=[
                "phase == 'reverse_left'",
                "abs(car_angle) > 3"
            ],
            actions={
                "throttle": "-config.max_reverse_speed * 0.25",
                "steering": "-12 - car_angle * 0.1",
                "reasoning": "Выравнивание задним ходом",
                "emergency": False
            },
            description="Выравнивание автомобиля"
        )

        # === ПРАВИЛА ПО УМОЛЧАНИЮ ===

        # Правило 14: Остановка по умолчанию
        rules["default_stop"] = Rule(
            name="Остановка по умолчанию",
            priority=RulePriority.LOW,
            conditions=["phase != 'parked'"],  # Не срабатывает когда уже припарковались
            actions={
                "throttle": 0.0,
                "steering": 0.0,
                "reasoning": "Ожидание команд",
                "emergency": False
            },
            description="Правило по умолчанию"
        )

        # Правило 15: Парковка завершена
        rules["parking_complete"] = Rule(
            name="Парковка завершена",
            priority=RulePriority.MEDIUM,
            conditions=["phase == 'parked'"],
            actions={
                "throttle": 0.0,
                "steering": 0.0,
                "reasoning": "Парковка успешно завершена!",
                "emergency": False
            },
            description="Завершение парковки"
        )

        return rules

    def update_facts(self, **facts):
        """Обновление фактов в базе знаний"""
        self.facts.update(facts)

        # Добавляем вычисляемые факты
        if 'car_x' in facts and 'selected_spot' in facts and facts['selected_spot']:
            self.facts['distance_to_spot'] = math.hypot(
                facts['car_x'] - facts['selected_spot'].x,
                facts['car_y'] - facts['selected_spot'].y
            )

    def infer(self) -> Dict[str, Any]:
        """Логический вывод на основе правил"""
        # Добавляем конфиг и константы в факты
        self.facts['config'] = self.config
        self.facts['CAR_LENGTH'] = CAR_LENGTH
        self.facts['CAR_WIDTH'] = CAR_WIDTH

        # Проверяем наличие необходимых фактов
        if 'throttle' not in self.facts:
            self.facts['throttle'] = 0.0
        if 'steering' not in self.facts:
            self.facts['steering'] = 0.0
        if 'emergency' not in self.facts:
            self.facts['emergency'] = False

        # Сортируем правила по приоритету
        sorted_rules = sorted(self.rules.values(),
                              key=lambda r: r.priority.value,
                              reverse=True)

        # Применяем правила
        for rule in sorted_rules:
            if rule.evaluate(self.facts):
                # print(f"✓ Правило применено: {rule.name}")

                actions = rule.actions.copy()

                # Обрабатываем выражения в действиях
                for key, value in actions.items():
                    if isinstance(value, str) and any(char in value for char in ['+', '-', '*', '/', '(', ')']):
                        try:
                            # Создаем контекст для eval
                            eval_context = {
                                'config': self.config,
                                'CAR_LENGTH': CAR_LENGTH,
                                'CAR_WIDTH': CAR_WIDTH,
                                'math': math
                            }
                            eval_context.update(self.facts)

                            # Вычисляем выражение
                            actions[key] = eval(value, {"__builtins__": {}}, eval_context)
                        except:
                            # Если не удалось вычислить, оставляем как есть
                            pass

                actions["rule_applied"] = rule.name
                actions["rule_description"] = rule.description

                return actions

        # Если ни одно правило не применилось
        return {
            "throttle": 0.0,
            "steering": 0.0,
            "reasoning": "Ожидание...",
            "emergency": False,
            "rule_applied": "none",
            "rule_description": "Правило не найдено"
        }


class ExpertSystem:
    """Экспертная система для принятия решений при парковке"""

    def __init__(self, config: ParkingConfig):
        self.config = config
        self.knowledge_base = KnowledgeBase(config)
        self.decision_history = []

    def make_decision(self, phase: ParkingPhase, sensors: List[float],
                      car_pos: Tuple[float, float], car_angle: float,
                      target_spot: Optional[ParkingSpot] = None) -> DecisionInfo:
        """Принятие решения на основе логического вывода"""

        # Подготавливаем факты
        facts = {
            'phase': phase.value,
            'car_x': car_pos[0],
            'car_y': car_pos[1],
            'car_angle': car_angle,
            'selected_spot': target_spot,

            # Датчики
            'front_left_sensor': sensors[2],
            'front_sensor': sensors[3],
            'front_right_sensor': sensors[4],
            'left_sensor': sensors[1],
            'right_sensor': sensors[5],
            'rear_left_sensor': sensors[0],
            'rear_right_sensor': sensors[6],
            'rear_sensor': sensors[7],
        }

        # Обновляем факты
        self.knowledge_base.update_facts(**facts)

        # Получаем решение
        decision = self.knowledge_base.infer()

        # Логируем решение
        self.decision_history.append({
            'phase': phase.value,
            'decision': decision.copy(),
            'sensors': sensors.copy(),
            'position': car_pos,
            'angle': car_angle,
            'rule_applied': decision.get('rule_applied', 'unknown')
        })

        return DecisionInfo(**decision)


class Car:
    def __init__(self, x, y, angle=0, color=(0, 100, 255)):
        self.x = x
        self.y = y
        self.angle = angle
        self.speed = 0.0
        self.max_speed = 2.5
        self.steering = 0.0
        self.color = color
        self.turning_radius = CAR_LENGTH * 2.5
        self.sensors = [SENSOR_RANGE] * 8
        self.previous_speed = 0.0
        self.acceleration = 0.01
        self.deceleration = 0.02

    def update(self, dt):
        # Плавное изменение скорости
        speed_diff = self.speed - self.previous_speed
        
        if speed_diff != 0:
            is_accelerating = abs(self.speed) > abs(self.previous_speed)
            
            if is_accelerating:
                max_change = self.acceleration * dt * 60
            else:
                max_change = self.deceleration * dt * 60
            
            # Ограничиваем изменение скорости
            if abs(speed_diff) > max_change:
                if speed_diff > 0:
                    self.speed = self.previous_speed + max_change
                else:
                    self.speed = self.previous_speed - max_change

        self.previous_speed = self.speed

        # Обновление угла
        if abs(self.steering) > 0.1 and abs(self.speed) > 0.1:
            turning_circle = self.turning_radius / max(0.1, abs(self.steering) / 30.0)
            angular_speed = self.speed / turning_circle

            if self.steering < 0:
                angular_speed = -angular_speed

            self.angle += math.degrees(angular_speed) * dt * 60

        # Обновление позиции
        dx = self.speed * math.cos(math.radians(self.angle)) * dt * 100
        dy = self.speed * math.sin(math.radians(self.angle)) * dt * 100
        self.x += dx
        self.y += dy

    def get_corners(self):
        c, s = math.cos(math.radians(self.angle)), math.sin(math.radians(self.angle))
        points = [
            (-CAR_LENGTH / 2, -CAR_WIDTH / 2),
            (CAR_LENGTH / 2, -CAR_WIDTH / 2),
            (CAR_LENGTH / 2, CAR_WIDTH / 2),
            (-CAR_LENGTH / 2, CAR_WIDTH / 2)
        ]
        return [(self.x + x * c - y * s, self.y + x * s + y * c) for x, y in points]

    def draw(self, screen):
        pygame.draw.polygon(screen, self.color, self.get_corners())

        # Рисование направления
        front = (
            self.x + CAR_LENGTH * 0.4 * math.cos(math.radians(self.angle)),
            self.y + CAR_LENGTH * 0.4 * math.sin(math.radians(self.angle))
        )
        pygame.draw.line(screen, (255, 255, 255), (self.x, self.y), front, 3)

        # Рисование сенсоров
        sensor_angles = [-135, -90, -45, -20, 20, 45, 90, 135]
        for i, da in enumerate(sensor_angles):
            ang = math.radians(self.angle + da)
            end = (
                self.x + self.sensors[i] * math.cos(ang),
                self.y + self.sensors[i] * math.sin(ang)
            )
            color = (255, 100, 100) if self.sensors[i] < 80 else (100, 255, 100) if self.sensors[i] < 150 else (100,
                                                                                                                150,
                                                                                                                255)
            pygame.draw.line(screen, color, (self.x, self.y), end, 2)

    def update_sensors(self, obstacles):
        """Обновление показаний сенсоров"""
        self.sensors = [SENSOR_RANGE] * 8
        sensor_angles = [-135, -90, -45, -20, 20, 45, 90, 135]

        for i, da in enumerate(sensor_angles):
            ang = math.radians(self.angle + da)
            dx, dy = math.cos(ang), math.sin(ang)

            for obs in obstacles:
                for corner in obs.get_corners():
                    px, py = corner[0] - self.x, corner[1] - self.y
                    proj = px * dx + py * dy

                    if proj > 0:
                        dist = math.hypot(px, py)
                        if dist < self.sensors[i]:
                            self.sensors[i] = dist

        return self.sensors


class ParkingSimulation:
    def __init__(self):
        pygame.init()
        self.screen = pygame.display.set_mode((WIDTH, HEIGHT))
        pygame.display.set_caption("ЭКСПЕРТНАЯ СИСТЕМА ПАРКОВКИ - РАБОЧАЯ ВЕРСИЯ")
        self.clock = pygame.time.Clock()
        self.font = pygame.font.SysFont("Arial", 22)
        self.small_font = pygame.font.SysFont("Arial", 16)

        # Конфигурация
        config = ParkingConfig(
            turn_radius_ratio=2.5,
            sensor_range=SENSOR_RANGE,
            max_speed=2.5,
            max_reverse_speed=1.5,
            safety_margin=30.0,
            steering_sensitivity=0.3
        )

        # Экспертная система
        self.expert_system = ExpertSystem(config)

        # Парковочные места
        self.parking_spots = self._create_parking_spots()

        # Припаркованные машины
        self.obstacle_cars = [
            Car(600, HEIGHT - 150, 0, (200, 0, 0)),
            Car(900, HEIGHT - 150, 0, (200, 0, 0)),
        ]

        # Генерируем случайные машины - заменяет предыдущую секцию
        self.obstacle_cars = self.generate_random_cars()

        # Основной автомобиль
        self.player_car = Car(200, HEIGHT - 200, 0)
        self.player_car.turning_radius = CAR_LENGTH * config.turn_radius_ratio

        # Состояние системы
        self.current_phase = ParkingPhase.SEARCHING
        self.selected_spot = None
        self.parked = False
        self.decision_info: Optional[DecisionInfo] = None
        self.phase_timer = 0
        self.current_sensors = [SENSOR_RANGE] * 8
        self.rule_applied = "Начало движения"
        self.start_time = pygame.time.get_ticks()

        # Статистика
        self.rules_applied_count = {}

        print("=" * 60)
        print("ЭКСПЕРТНАЯ СИСТЕМА ПАРКОВКИ ЗАПУЩЕНА")
        print("=" * 60)
        print("Ожидайте начала движения...")
        print("=" * 60)

    def _create_parking_spots(self) -> List[ParkingSpot]:
        """Создание парковочных мест"""
        spots = []
        spot_positions = [400, 550, 700, 850, 1000, 1150]

        for i, x in enumerate(spot_positions):
            occupied = (i == 1 or i == 3)  # 2 занятых места
            spots.append(ParkingSpot(
                x=x,
                y=HEIGHT - 150,
                width=100,
                length=120,
                occupied=occupied,
                suitable=not occupied
            ))

        return spots

    def generate_random_cars(self):
        """Создание машин в случайных парковочных местах"""
        Vector3 = Tuple[float, float, float]

        PARKING_FILLING = 0.7
        MAX_OFFSET = 5

        obstacle_cars = []
        
        for spot in self.parking_spots:
            if random.random() < PARKING_FILLING:
                offset = (
                    random.uniform(-MAX_OFFSET, MAX_OFFSET),
                    random.uniform(-MAX_OFFSET, MAX_OFFSET)
                )
                final_pos = (spot.x + offset[0],
                             spot.y + offset[1])
                
                car = Car(final_pos[0], final_pos[1], 0, (200, 0, 0))
                obstacle_cars.append(car)
                
        return obstacle_cars


    def find_best_parking_spot(self) -> Optional[ParkingSpot]:
        """Поиск наилучшего парковочного места"""
        available_spots = [spot for spot in self.parking_spots if not spot.occupied]

        if not available_spots:
            return None

        # Выбираем место подальше от препятствий
        best_spot = None
        max_distance = 0

        for spot in available_spots:
            min_dist_to_obstacle = float('inf')
            for obs in self.obstacle_cars:
                dist = abs(obs.x - spot.x)
                if dist < min_dist_to_obstacle:
                    min_dist_to_obstacle = dist

            if min_dist_to_obstacle > max_distance:
                max_distance = min_dist_to_obstacle
                best_spot = spot

        if best_spot:
            print(f"✓ Найдено парковочное место: X={best_spot.x}")

        return best_spot

    def run(self):
        running = True
        while running:
            dt = self.clock.tick(FPS) / 1000.0
            self.phase_timer += dt

            # Обработка событий
            for event in pygame.event.get():
                if event.type == pygame.QUIT:
                    running = False
                elif event.type == pygame.KEYDOWN:
                    if event.key == pygame.K_r:
                        print("\n" + "=" * 60)
                        print("ПЕРЕЗАПУСК СИСТЕМЫ...")
                        print("=" * 60)
                        self.__init__()
                        continue
                    elif event.key == pygame.K_s:
                        self.save_decision_log()
                    elif event.key == pygame.K_SPACE:
                        self.manual_phase_transition()
                    elif event.key == pygame.K_p:
                        self.print_rules_info()
                    elif event.key == pygame.K_d:
                        self.debug_info()

            # Обновление сенсоров
            all_obstacles = self.obstacle_cars.copy()
            self.current_sensors = self.player_car.update_sensors(all_obstacles)

            # Автоматический поиск места
            if self.current_phase == ParkingPhase.SEARCHING and not self.selected_spot:
                if self.phase_timer > 0.5 and self.player_car.x > 250:
                    self.selected_spot = self.find_best_parking_spot()

            # Принятие решения
            self.decision_info = self.expert_system.make_decision(
                self.current_phase,
                self.current_sensors,
                (self.player_car.x, self.player_car.y),
                self.player_car.angle,
                self.selected_spot
            )

            # Статистика правил
            rule_name = self.decision_info.get("rule_applied", "unknown")
            self.rule_applied = rule_name
            if rule_name in self.rules_applied_count:
                self.rules_applied_count[rule_name] += 1
            else:
                self.rules_applied_count[rule_name] = 1

            # Применение решения
            if self.decision_info.get("emergency", False):
                self.player_car.speed = 0.0
                self.player_car.steering = 0.0
            else:
                self.player_car.speed = float(self.decision_info["throttle"])
                self.player_car.steering = float(self.decision_info["steering"])

            # Обновление автомобиля
            self.player_car.update(dt)

            # Автоматические переходы фаз
            self.handle_phase_transitions()

            # Отрисовка
            self.draw()

            if self.parked:
                self.draw_success_message()

            pygame.display.flip()

        pygame.quit()
        sys.exit()

    def handle_phase_transitions(self):
        """Обработка переходов между фазами"""

        if self.current_phase == ParkingPhase.SEARCHING:
            if self.selected_spot and self.player_car.x > self.selected_spot.x - 50:
                self.current_phase = ParkingPhase.APPROACH
                self.phase_timer = 0
                print(f"\n⇨ Переход к фазе: ПОДЪЕЗД")
                print(f"   Цель: X={self.selected_spot.x}")

        elif self.current_phase == ParkingPhase.APPROACH:
            if self.player_car.x > self.selected_spot.x + CAR_LENGTH * 1.2:
                self.current_phase = ParkingPhase.POSITIONING
                self.phase_timer = 0
                print(f"\n⇨ Переход к фазе: ПОЗИЦИОНИРОВАНИЕ")

        elif self.current_phase == ParkingPhase.POSITIONING:
            if self.phase_timer > 3.0:
                self.current_phase = ParkingPhase.PREPARE_REVERSE
                self.phase_timer = 0
                print(f"\n⇨ Переход к фазе: ПОДГОТОВКА К РЕВЕРСУ")

        elif self.current_phase == ParkingPhase.PREPARE_REVERSE:
            if self.phase_timer > 0.5:
                self.current_phase = ParkingPhase.REVERSE_RIGHT
                self.phase_timer = 0
                print(f"\n⇨ Переход к фазе: ЗАДНИЙ МАНЕВР (ВПРАВО)")

        elif self.current_phase == ParkingPhase.REVERSE_RIGHT:
            if self.player_car.angle < -25:
                self.current_phase = ParkingPhase.REVERSE_LEFT
                self.phase_timer = 0
                print(f"\n⇨ Переход к фазе: ВЫРАВНИВАНИЕ (ВЛЕВО)")

        elif self.current_phase == ParkingPhase.REVERSE_LEFT:
            if abs(self.player_car.angle) < 5 or self.phase_timer > 4.0:
                self.current_phase = ParkingPhase.FINAL_ADJUST
                self.phase_timer = 0
                print(f"\n⇨ Переход к фазе: ФИНАЛЬНАЯ КОРРЕКТИРОВКА")

        elif self.current_phase == ParkingPhase.FINAL_ADJUST:
            if self.phase_timer > 3.0:
                self.current_phase = ParkingPhase.PARKED
                self.parked = True
                print(f"\n" + "=" * 60)
                print("✓ ПАРКОВКА УСПЕШНО ЗАВЕРШЕНА!")
                print("=" * 60)

    def manual_phase_transition(self):
        """Ручной переход фаз"""
        phases = list(ParkingPhase)
        current_index = phases.index(self.current_phase)
        next_index = (current_index + 1) % (len(phases) - 1)
        old_phase = self.current_phase.value
        self.current_phase = phases[next_index]
        self.phase_timer = 0
        print(f"\n⇨ Ручной переход: {old_phase} → {self.current_phase.value}")

    def print_rules_info(self):
        """Вывод информации о правилах"""
        print("\n" + "=" * 60)
        print("СТАТИСТИКА ПРИМЕНЕНИЯ ПРАВИЛ:")
        print("=" * 60)

        total = sum(self.rules_applied_count.values())
        for rule_name, count in sorted(self.rules_applied_count.items(),
                                       key=lambda x: x[1], reverse=True):
            percentage = (count / total) * 100
            print(f"  {rule_name}: {count} раз ({percentage:.1f}%)")

        print(f"\nВсего решений: {total}")
        print("=" * 60)

    def debug_info(self):
        """Отладочная информация"""
        print("\n" + "=" * 60)
        print("ОТЛАДОЧНАЯ ИНФОРМАЦИЯ:")
        print("=" * 60)
        print(f"Фаза: {self.current_phase.value}")
        print(f"Время фазы: {self.phase_timer:.1f}с")
        print(f"Позиция: ({self.player_car.x:.0f}, {self.player_car.y:.0f})")
        print(f"Угол: {self.player_car.angle:.1f}°")
        print(f"Скорость: {self.player_car.speed:.2f}")
        print(f"Руль: {self.player_car.steering:.1f}°")

        if self.selected_spot:
            dx = self.selected_spot.x - self.player_car.x
            dy = self.selected_spot.y - self.player_car.y
            dist = math.hypot(dx, dy)
            print(f"Цель: X={self.selected_spot.x}, расстояние: {dist:.0f}")

        print(f"Активное правило: {self.rule_applied}")
        print("=" * 60)

    def draw(self):
        """Отрисовка всей сцены"""
        self.screen.fill((30, 30, 30))

        # Дорога
        pygame.draw.rect(self.screen, (70, 70, 70),
                         (0, HEIGHT - 320, WIDTH, 270))

        # Разметка
        for i in range(0, WIDTH, 60):
            pygame.draw.line(self.screen, (255, 255, 200),
                             (i, HEIGHT - 200), (i + 30, HEIGHT - 200), 3)

        # Бордюр
        pygame.draw.line(self.screen, (200, 200, 200),
                         (0, HEIGHT - 150), (WIDTH, HEIGHT - 150), 3)

        # Парковочные места
        for spot in self.parking_spots:
            color = (0, 220, 0) if not spot.occupied else (220, 50, 50)
            if spot == self.selected_spot:
                color = (255, 255, 0)

            pygame.draw.rect(self.screen, color,
                             (spot.x - spot.width // 2, spot.y - spot.length // 2,
                              spot.width, spot.length), 3)

            if not spot.occupied:
                p_text = self.small_font.render("P", True, (0, 220, 0))
                self.screen.blit(p_text, (spot.x - 5, spot.y - 10))

            if spot == self.selected_spot:
                target_text = self.small_font.render("ЦЕЛЬ", True, (255, 255, 0))
                self.screen.blit(target_text, (spot.x - 15, spot.y - 80))

        # Препятствия
        for car in self.obstacle_cars:
            car.draw(self.screen)

        # Основной автомобиль
        self.player_car.draw(self.screen)

        # Визуализация цели
        if self.selected_spot:
            # Линия к цели
            pygame.draw.line(self.screen, (255, 255, 0, 150),
                             (self.player_car.x, self.player_car.y),
                             (self.selected_spot.x, self.selected_spot.y), 2)

            # Зона позиционирования
            pos_y = self.selected_spot.y - CAR_WIDTH * 1.5
            pygame.draw.line(self.screen, (255, 200, 0, 100),
                             (self.selected_spot.x - 100, pos_y),
                             (self.selected_spot.x + 200, pos_y), 2)

            # Целевая точка
            target_x = self.selected_spot.x + CAR_LENGTH * 1.5
            pygame.draw.circle(self.screen, (255, 100, 0),
                               (int(target_x), int(pos_y)), 6)

        # Интерфейс
        self.draw_expert_system_ui()

    def draw_expert_system_ui(self):
        """Отрисовка интерфейса"""
        # Фон для интерфейса
        pygame.draw.rect(self.screen, (40, 40, 50, 200), (0, 0, WIDTH, 180))

        y_offset = 20

        # Заголовок
        title = self.font.render("ЭКСПЕРТНАЯ СИСТЕМА ПАРКОВКИ - В РАБОТЕ", True, (255, 255, 200))
        self.screen.blit(title, (WIDTH // 2 - title.get_width() // 2, y_offset))
        y_offset += 40

        # Текущая фаза
        phase_text = self.font.render(f"ФАЗА: {self.current_phase.value.upper()}", True, (255, 200, 100))
        self.screen.blit(phase_text, (50, y_offset))

        # Время фазы
        time_text = self.small_font.render(f"Время: {self.phase_timer:.1f}с", True, (200, 200, 200))
        self.screen.blit(time_text, (WIDTH - 150, y_offset))
        y_offset += 35

        # Активное правило
        rule_box = pygame.Rect(50, y_offset, WIDTH - 100, 60)
        pygame.draw.rect(self.screen, (60, 70, 60), rule_box, border_radius=5)
        pygame.draw.rect(self.screen, (100, 150, 100), rule_box, 2, border_radius=5)

        rule_title = self.small_font.render("АКТИВНОЕ ПРАВИЛО:", True, (200, 255, 200))
        self.screen.blit(rule_title, (70, y_offset + 10))

        rule_name = self.font.render(f"{self.rule_applied}", True, (220, 255, 220))
        self.screen.blit(rule_name, (70, y_offset + 30))
        y_offset += 70

        # Левая колонка - управление
        left_col = 50
        control_text = self.small_font.render("УПРАВЛЕНИЕ:", True, (200, 220, 255))
        self.screen.blit(control_text, (left_col, y_offset))

        speed_color = (200, 255, 200) if self.player_car.speed > 0 else (255, 200,
                                                                         200) if self.player_car.speed < 0 else (200,
                                                                                                                 200,
                                                                                                                 200)
        speed_text = self.small_font.render(f"Скорость: {self.player_car.speed:.2f}", True, speed_color)
        self.screen.blit(speed_text, (left_col + 20, y_offset + 25))

        steer_text = self.small_font.render(f"Руль: {self.player_car.steering:.1f}°", True, (220, 220, 200))
        self.screen.blit(steer_text, (left_col + 20, y_offset + 45))

        # Правая колонка - обоснование
        right_col = WIDTH // 2 + 50
        reason_text = self.small_font.render("ОБОСНОВАНИЕ:", True, (255, 220, 200))
        self.screen.blit(reason_text, (right_col, y_offset))

        reasoning = self.decision_info.get("reasoning", "Ожидание...")
        if len(reasoning) > 35:
            reasoning = reasoning[:35] + "..."
        reason_content = self.small_font.render(reasoning, True, (255, 240, 200))
        self.screen.blit(reason_content, (right_col + 20, y_offset + 25))

        # Позиция автомобиля (правый верхний угол)
        pos_y = 20
        pos_box = pygame.Rect(WIDTH - 300, pos_y, 280, 100)
        pygame.draw.rect(self.screen, (50, 50, 70, 200), pos_box)
        pygame.draw.rect(self.screen, (100, 100, 150), pos_box, 2)

        pos_title = self.small_font.render("ПОЗИЦИЯ АВТО:", True, (200, 220, 255))
        self.screen.blit(pos_title, (WIDTH - 280, pos_y + 10))

        pos_info = [
            f"X: {self.player_car.x:.0f}",
            f"Y: {self.player_car.y:.0f}",
            f"Угол: {self.player_car.angle:.1f}°"
        ]

        for i, line in enumerate(pos_info):
            line_text = self.small_font.render(line, True, (220, 220, 240))
            self.screen.blit(line_text, (WIDTH - 280, pos_y + 35 + i * 18))

        # Датчики
        sensor_y = pos_y + 110
        sensor_box = pygame.Rect(WIDTH - 300, sensor_y, 280, 120)
        pygame.draw.rect(self.screen, (50, 70, 50, 200), sensor_box)
        pygame.draw.rect(self.screen, (100, 150, 100), sensor_box, 2)

        sensor_title = self.small_font.render("ДАКТЧИКИ:", True, (200, 255, 200))
        self.screen.blit(sensor_title, (WIDTH - 280, sensor_y + 10))

        sensor_names = ["Перед", "Прав", "Зад.пр", "Зад"]
        sensor_values = [
            self.current_sensors[3],  # Передний
            self.current_sensors[5],  # Правый
            self.current_sensors[6],  # Задний правый
            self.current_sensors[7]   # Задний
        ]

        for i, (name, value) in enumerate(zip(sensor_names, sensor_values)):
            sensor_x = WIDTH - 280 + (i % 2) * 120
            sensor_y_pos = sensor_y + 35 + (i // 2) * 25

            if value < 80:
                color = (255, 150, 150)
            elif value < 150:
                color = (255, 255, 150)
            else:
                color = (150, 255, 150)

            sensor_text = self.small_font.render(f"{name}: {value:.0f}", True, color)
            self.screen.blit(sensor_text, (sensor_x, sensor_y_pos))

        # Управление внизу
        controls_y = HEIGHT - 70
        pygame.draw.rect(self.screen, (40, 40, 60), (0, controls_y, WIDTH, 70))

        controls = [
            "R - ПЕРЕЗАПУСК",
            "S - СОХРАНИТЬ ЛОГ",
            "SPACE - СЛЕД.ФАЗА",
            "P - СТАТИСТИКА",
            "D - ОТЛАДКА"
        ]

        control_width = WIDTH // len(controls)
        for i, control in enumerate(controls):
            control_text = self.small_font.render(control, True, (180, 200, 220))
            x_pos = i * control_width + control_width // 2 - control_text.get_width() // 2
            self.screen.blit(control_text, (x_pos, controls_y + 15))

        # Статистика
        total_time = (pygame.time.get_ticks() - self.start_time) / 1000.0
        stats_text = self.small_font.render(
            f"Решений: {len(self.expert_system.decision_history)} | " +
            f"Время: {total_time:.1f}с | " +
            f"Фаза: {self.phase_timer:.1f}с",
            True, (150, 180, 200)
        )
        self.screen.blit(stats_text, (WIDTH // 2 - stats_text.get_width() // 2, controls_y + 40))

    def draw_success_message(self):
        """Отрисовка сообщения об успешной парковке"""
        overlay = pygame.Surface((WIDTH, HEIGHT), pygame.SRCALPHA)
        overlay.fill((0, 0, 0, 150))
        self.screen.blit(overlay, (0, 0))

        success_box = pygame.Rect(WIDTH // 2 - 250, HEIGHT // 2 - 100, 500, 200)
        pygame.draw.rect(self.screen, (30, 60, 30), success_box, border_radius=10)
        pygame.draw.rect(self.screen, (100, 200, 100), success_box, 4, border_radius=10)

        success_text = self.font.render("ПАРКОВКА ЗАВЕРШЕНА!",
                                        True, (100, 255, 100))
        self.screen.blit(success_text,
                         (WIDTH // 2 - success_text.get_width() // 2, HEIGHT // 2 - 70))

        rules_used = len(set([d.get("rule_applied") for d in self.expert_system.decision_history]))
        total_time = (pygame.time.get_ticks() - self.start_time) / 1000.0

        stats_lines = [
            f"Использовано правил: {rules_used}",
            f"Всего решений: {len(self.expert_system.decision_history)}",
            f"Общее время: {total_time:.1f} секунд"
        ]

        for i, line in enumerate(stats_lines):
            line_text = self.small_font.render(line, True, (200, 255, 200))
            self.screen.blit(line_text,
                             (WIDTH // 2 - line_text.get_width() // 2, HEIGHT // 2 - 30 + i * 25))

        restart_text = self.small_font.render(
            "Нажмите R для новой парковки",
            True, (150, 255, 150)
        )
        self.screen.blit(restart_text,
                         (WIDTH // 2 - restart_text.get_width() // 2, HEIGHT // 2 + 50))

    def save_decision_log(self):
        """Сохранение лога решений"""
        try:
            filename = f"parking_log_{pygame.time.get_ticks()}.json"
            with open(filename, "w", encoding="utf-8") as f:
                log_data = []
                for entry in self.expert_system.decision_history:
                    log_entry = {
                        "phase": entry["phase"],
                        "rule": entry.get("rule_applied", "unknown"),
                        "reasoning": entry["decision"].get("reasoning", ""),
                        "throttle": entry["decision"].get("throttle", 0),
                        "steering": entry["decision"].get("steering", 0),
                        "position": entry["position"],
                        "angle": entry["angle"]
                    }
                    log_data.append(log_entry)

                json.dump(log_data, f, indent=2, ensure_ascii=False)
                print(f"\n✓ Лог сохранен в файл: {filename}")
                print(f"   Записей: {len(log_data)}")

        except Exception as e:
            print(f"\n✗ Ошибка сохранения: {e}")


def main():
    simulation = ParkingSimulation()
    simulation.run()


if __name__ == "__main__":
    main()