<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';

type StreamGoalStatus = 'pending' | 'in-progress' | 'complete';
type SprintGoalMood = 'focus' | 'stretch' | 'restore';

interface StreamGoal {
	label: string;
	detail: string;
	status: StreamGoalStatus;
}

interface SprintGoal {
	label: string;
	mood: SprintGoalMood;
	status: StreamGoalStatus;
}

interface ChatMessage {
	id: number;
	user: string;
	role?: 'mod' | 'vip' | 'broadcaster';
	color: string;
	message: string;
	timestamp: string;
}

interface InfoMetric {
	label: string;
	value: string;
	detail?: string;
	tone?: 'positive' | 'warning' | 'neutral';
}

interface InfoSlide {
	id: string;
	title: string;
	caption: string;
	metrics: InfoMetric[];
}

interface StreamAlert {
	id: number;
	icon: string;
	user: string;
	type: 'follow' | 'sub' | 'raid' | 'redeem';
	detail: string;
	since: string;
	accent: string;
}

const streamTitle = 'Night Shift Code & Chill';
const streamSubtitle = 'Synthwave cowork + overlay build';
const streamTagline = 'Ambient neon • Live coding & overlay polish';
const streamTimeRemaining = '2h 24m';
const streamFocusTags = ['Cyberpunk UI', 'Chill synth', 'Neon vibes'];

const streamGoals: StreamGoal[] = [
	{
		label: 'Polish overlay layout',
		detail: 'Align neon panels & responsive spacing',
		status: 'in-progress'
	},
	{
		label: 'Animate rotating info hub',
		detail: 'Slide transitions + light trails',
		status: 'pending'
	},
	{
		label: 'Integrate sprint timer visual',
		detail: 'Conic gradient progress disc',
		status: 'complete'
	}
];

const sprintNumber = 3;
const sprintTheme = 'Neon UI Polish';
const sprintMode = 'Deep build cycle';
const sprintDuration = 45;
const sprintTimeLeft = ref('23:17');
const sprintProgress = ref(0.52);

const sprintGoals: SprintGoal[] = [
	{
		label: 'Refine holo timer glow',
		mood: 'focus',
		status: 'in-progress'
	},
	{
		label: 'Mock alert capsule states',
		mood: 'stretch',
		status: 'pending'
	},
	{
		label: 'Balance color wave mix',
		mood: 'restore',
		status: 'pending'
	}
];

const sprintMoodLegend: Record<
	SprintGoalMood,
	{ glyph: string; label: string }
> = {
	focus: { glyph: '⚡', label: 'Focus' },
	stretch: { glyph: '🛠', label: 'Stretch' },
	restore: { glyph: '☕', label: 'Restore' }
};

const chatMessages: ChatMessage[] = [
	{
		id: 1,
		user: 'NeonRunner',
		role: 'mod',
		color: '#2FFFE0',
		message: 'Those holographic panels are unreal, loving the glow!',
		timestamp: '01:08'
	},
	{
		id: 2,
		user: 'EightBitMuse',
		color: '#FF3FD5',
		message: 'Requesting a pink gradient on the info hub ✨',
		timestamp: '01:06'
	},
	{
		id: 3,
		user: 'GhostCircuit',
		role: 'vip',
		color: '#FFB02E',
		message: 'Timer core looks crisp now. Chromatic fringe is on point.',
		timestamp: '01:04'
	},
	{
		id: 4,
		user: 'SynthPilot',
		color: '#6A4BFF',
		message: 'Queue up more outrun beats while you tweak that HUD!',
		timestamp: '01:02'
	}
];

const roleBadges: Record<'mod' | 'vip' | 'broadcaster', string> = {
	mod: 'MOD',
	vip: 'VIP',
	broadcaster: 'HOST'
};

const infoSlides: InfoSlide[] = [
	{
		id: 'stream-stats',
		title: 'Stream Stats',
		caption: 'Live cadence across the shift',
		metrics: [
			{ label: 'Viewers', value: '183', detail: '+12 past 10m', tone: 'positive' },
			{ label: 'Chat Pace', value: '44 / min', detail: 'Steady pulse', tone: 'neutral' },
			{ label: 'Followers Tonight', value: '+38', detail: 'Goal track: 72%', tone: 'positive' }
		]
	},
	{
		id: 'game-stats',
		title: 'Game Stats',
		caption: 'Citadel Protocol run · NG+',
		metrics: [
			{ label: 'Current Mission', value: 'Data Vault Breach', detail: 'Phase 2 of 3', tone: 'neutral' },
			{ label: 'Completion', value: '67%', detail: 'Projected finish 02:55', tone: 'positive' },
			{ label: 'Build', value: 'Cipher Runner v4', detail: 'EMP & hacking focus', tone: 'neutral' }
		]
	},
	{
		id: 'schedule',
		title: 'Schedule',
		caption: 'Upcoming beats for the neon shift',
		metrics: [
			{ label: 'Break', value: '02:00', detail: 'Hydrate & stretch', tone: 'warning' },
			{ label: 'Guest Drop', value: '02:30', detail: 'PixelParadox collab', tone: 'neutral' },
			{ label: 'Afterparty', value: '03:45', detail: 'Synthwave DJ set', tone: 'neutral' }
		]
	}
];

const alerts: StreamAlert[] = [
	{
		id: 1,
		icon: '🌟',
		user: 'AstraPulse',
		type: 'sub',
		detail: 'Tier 3 resub · 12 months',
		since: '2m ago',
		accent: '#FF3FD5'
	},
	{
		id: 2,
		icon: '🚀',
		user: 'QuantumRaid',
		type: 'raid',
		detail: 'Raid · 146 explorers',
		since: '6m ago',
		accent: '#2FFFE0'
	},
	{
		id: 3,
		icon: '💎',
		user: 'Glitchling',
		type: 'redeem',
		detail: 'Channel redeem · Neon Wave',
		since: '9m ago',
		accent: '#FFB02E'
	},
	{
		id: 4,
		icon: '✨',
		user: 'NightPixel',
		type: 'follow',
		detail: 'New follow',
		since: '12m ago',
		accent: '#6A4BFF'
	}
];

const activePanelIndex = ref(0);
const panelRotationMs = 12000;
const rotationSeconds = Math.round(panelRotationMs / 1000);
let rotationTimer: number | undefined;

const startRotation = () => {
	if (typeof window === 'undefined') {
		return;
	}

	if (rotationTimer !== undefined) {
		window.clearInterval(rotationTimer);
	}

	rotationTimer = window.setInterval(() => {
		activePanelIndex.value = (activePanelIndex.value + 1) % infoSlides.length;
	}, panelRotationMs);
};

const goToPanel = (index: number) => {
	activePanelIndex.value = index;
	startRotation();
};

onMounted(() => {
	startRotation();
});

onBeforeUnmount(() => {
	if (rotationTimer !== undefined) {
		window.clearInterval(rotationTimer);
	}
});

const activePanel = computed(() => infoSlides[activePanelIndex.value]);
const sprintProgressAngle = computed(() => `${Math.min(Math.max(sprintProgress.value, 0), 1) * 360}deg`);
const sprintProgressPercent = computed(() => Math.round(Math.min(Math.max(sprintProgress.value, 0), 1) * 100));
</script>

<template>
	<main class="overlay" aria-labelledby="overlay-title">
		<div class="overlay__flux overlay__flux--one"></div>
		<div class="overlay__flux overlay__flux--two"></div>
		<div class="overlay__grain"></div>

		<div class="overlay-grid">
			<section class="panel stream-status" aria-label="Stream status">
				<header class="panel__header">
					<h1 id="overlay-title" class="panel__title">{{ streamTitle }}</h1>
					<p class="panel__subtitle">{{ streamSubtitle }}</p>
					<span class="panel__tagline">{{ streamTagline }}</span>
				</header>

				<div class="stream-status__meta">
					<div class="meta-block">
						<span class="meta-label">Time Remaining</span>
						<span class="meta-value">{{ streamTimeRemaining }}</span>
					</div>
					<ul class="meta-tags" role="list">
						<li v-for="tag in streamFocusTags" :key="tag">{{ tag }}</li>
					</ul>
				</div>

				<div class="stream-goals">
					<h2 class="section-title">Stream Goals</h2>
					<ul role="list">
						<li
							v-for="goal in streamGoals"
							:key="goal.label"
							class="stream-goal"
							:data-status="goal.status"
						>
							<div class="stream-goal__indicator"></div>
							<div class="stream-goal__body">
								<span class="stream-goal__label">{{ goal.label }}</span>
								<span class="stream-goal__detail">{{ goal.detail }}</span>
							</div>
						</li>
					</ul>
				</div>
			</section>

			<section class="panel sprint-panel" aria-label="Current sprint">
				<header class="panel__header">
					<h2 class="panel__title">Current Sprint {{ String(sprintNumber).padStart(2, '0') }}</h2>
					<p class="panel__subtitle">{{ sprintTheme }}</p>
					<span class="panel__tagline">{{ sprintMode }} · {{ sprintDuration }} minute cycle</span>
				</header>

				<div class="sprint-panel__summary">
					<div class="summary-card">
						<span class="summary-label">Time Left</span>
						<span class="summary-value">{{ sprintTimeLeft }}</span>
					</div>
					<div class="summary-card">
						<span class="summary-label">Progress</span>
						<span class="summary-value">{{ sprintProgressPercent }}%</span>
					</div>
				</div>

				<ul class="sprint-goals" role="list">
					<li
						v-for="goal in sprintGoals"
						:key="goal.label"
						class="sprint-goal"
						:data-status="goal.status"
						:data-mood="goal.mood"
					>
						<span class="sprint-goal__glyph">{{ sprintMoodLegend[goal.mood].glyph }}</span>
						<span class="sprint-goal__label">{{ goal.label }}</span>
						<span class="sprint-goal__status">{{ sprintMoodLegend[goal.mood].label }}</span>
					</li>
				</ul>
			</section>

			<section class="panel information-panel" aria-label="Information panel carousel">
				<header class="panel__header">
					<h2 class="panel__title">Information Hub</h2>
					<p class="panel__subtitle">Rotates every {{ rotationSeconds }} seconds</p>
				</header>

				<Transition name="panel-transition" mode="out-in">
					<div v-if="activePanel" :key="activePanel.id" class="info-slide">
						<h3 class="info-slide__title">{{ activePanel.title }}</h3>
						<p class="info-slide__caption">{{ activePanel.caption }}</p>
						<ul class="info-metrics" role="list">
							<li
								v-for="metric in activePanel.metrics"
								:key="metric.label"
								class="info-metric"
								:data-tone="metric.tone ?? 'neutral'"
							>
								<span class="info-metric__label">{{ metric.label }}</span>
								<span class="info-metric__value">{{ metric.value }}</span>
								<span v-if="metric.detail" class="info-metric__detail">{{ metric.detail }}</span>
							</li>
						</ul>
					</div>
				</Transition>

				<div class="info-dots" role="tablist" aria-label="Information slides">
					<button
						v-for="(panel, index) in infoSlides"
						:key="panel.id"
						class="info-dot"
						type="button"
						:aria-pressed="activePanelIndex === index"
						:aria-label="`Show ${panel.title}`"
						@click="goToPanel(index)"
						:data-active="activePanelIndex === index"
					>
						<span class="info-dot__glow"></span>
					</button>
				</div>
			</section>

			<section class="panel chat-panel" aria-label="Chat replay">
				<header class="panel__header">
					<h2 class="panel__title">Chat Stream</h2>
					<p class="panel__subtitle">Live feed aesthetic</p>
				</header>

				<ul class="chat-feed" role="list">
					<li v-for="message in chatMessages" :key="message.id" class="chat-line">
						<span class="chat-line__timestamp">{{ message.timestamp }}</span>
						<span class="chat-line__author" :style="{ color: message.color }">
							<span
								v-if="message.role"
								class="chat-line__badge"
								:data-role="message.role"
							>{{ roleBadges[message.role] }}</span>
							{{ message.user }}
						</span>
						<span class="chat-line__message">{{ message.message }}</span>
					</li>
				</ul>
			</section>

			<section class="panel alerts-panel" aria-label="Recent alerts">
				<header class="panel__header">
					<h2 class="panel__title">Alert Capsule</h2>
					<p class="panel__subtitle">History of hype</p>
				</header>

				<ul class="alerts-feed" role="list">
					<li v-for="alert in alerts" :key="alert.id" class="alert-line">
						<div class="alert-line__icon" :style="{ color: alert.accent }">{{ alert.icon }}</div>
						<div class="alert-line__body">
							<div class="alert-line__headline">
								<span class="alert-line__user">{{ alert.user }}</span>
								<span class="alert-line__type">{{ alert.type }}</span>
							</div>
							<p class="alert-line__detail">{{ alert.detail }}</p>
						</div>
						<span class="alert-line__time">{{ alert.since }}</span>
					</li>
				</ul>
			</section>

			<section class="panel sprint-timer" aria-label="Sprint timer">
				<header class="panel__header">
					<h2 class="panel__title">Sprint Timer</h2>
					<p class="panel__subtitle">Locked in & glowing</p>
				</header>

				<div class="timer-display" :style="{ '--progress-angle': sprintProgressAngle }">
					<div class="timer-ring">
						<div class="timer-core">
							<span class="timer-core__value">{{ sprintTimeLeft }}</span>
							<span class="timer-core__label">Remaining</span>
						</div>
					</div>
				</div>
				<p class="timer-footnote">Focus: {{ sprintTheme }} · Cycle {{ sprintNumber }}</p>
			</section>
		</div>
	</main>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Orbitron:wght@400;600&family=Rajdhani:wght@400;600&family=Share+Tech+Mono&display=swap');

:global(body) {
	margin: 0;
	background: #05010f;
	color: #f5f7ff;
	font-family: 'Rajdhani', 'Segoe UI', sans-serif;
}

.overlay {
	--accent-cyan: #2fffe0;
	--accent-magenta: #ff3fd5;
	--accent-amber: #ffb02e;
	--accent-violet: #6a4bff;
	--panel-bg: rgba(7, 4, 20, 0.72);
	--panel-edge: rgba(146, 92, 255, 0.45);
	--panel-glow: rgba(39, 244, 255, 0.35);
	--text-primary: rgba(248, 249, 255, 0.96);
	--text-muted: rgba(220, 214, 255, 0.62);
	position: relative;
	min-height: 100vh;
	display: flex;
	align-items: center;
	justify-content: center;
	padding: clamp(24px, 5vw, 64px);
	background: radial-gradient(120% 120% at 16% 20%, rgba(255, 63, 213, 0.28), transparent 62%),
		radial-gradient(120% 120% at 82% 78%, rgba(39, 244, 255, 0.18), transparent 55%),
		radial-gradient(140% 120% at 50% 100%, rgba(106, 75, 255, 0.2), transparent 60%),
		#05010f;
	overflow: hidden;
	letter-spacing: 0.04em;
}

.overlay__flux {
	position: absolute;
	width: 160%;
	height: 160%;
	top: -30%;
	left: -30%;
	filter: blur(120px);
	mix-blend-mode: screen;
	opacity: 0.55;
	pointer-events: none;
	z-index: 0;
}

.overlay__flux--one {
	background: radial-gradient(circle at 30% 30%, rgba(255, 63, 213, 0.4), transparent 65%);
	animation: driftOne 34s ease-in-out infinite alternate;
}

.overlay__flux--two {
	background: radial-gradient(circle at 70% 70%, rgba(39, 244, 255, 0.35), transparent 60%);
	animation: driftTwo 40s ease-in-out infinite alternate;
	opacity: 0.45;
}

.overlay__grain {
	position: absolute;
	inset: 0;
	background-image: linear-gradient(0deg, rgba(255, 255, 255, 0.03) 1px, transparent 1px);
	background-size: 100% 2px;
	mix-blend-mode: screen;
	opacity: 0.12;
	animation: scanlines 12s linear infinite;
	pointer-events: none;
	z-index: 0;
}

.overlay-grid {
	position: relative;
	z-index: 1;
	width: min(1280px, 100%);
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	grid-template-rows: auto auto auto auto;
	grid-template-areas:
		'status chat'
		'sprint chat'
		'info alerts'
		'timer timer';
	gap: clamp(18px, 2.8vw, 32px);
}

.stream-status {
	grid-area: status;
}

.sprint-panel {
	grid-area: sprint;
}

.information-panel {
	grid-area: info;
	display: flex;
	flex-direction: column;
	min-height: 100%;
}

.chat-panel {
	grid-area: chat;
}

.alerts-panel {
	grid-area: alerts;
}

.sprint-timer {
	grid-area: timer;
}

.panel {
	position: relative;
	padding: clamp(20px, 2.8vw, 32px);
	border-radius: 22px;
	background: linear-gradient(135deg, rgba(8, 4, 18, 0.86), rgba(16, 7, 38, 0.72));
	border: 1px solid rgba(148, 96, 255, 0.28);
	backdrop-filter: blur(18px);
	box-shadow: 0 12px 44px rgba(8, 0, 45, 0.6), 0 0 0 1px rgba(255, 255, 255, 0.03);
	overflow: hidden;
	isolation: isolate;
}

.panel::before {
	content: '';
	position: absolute;
	inset: 0;
	border-radius: inherit;
	background: linear-gradient(135deg, rgba(255, 63, 213, 0.15), transparent 45%, rgba(39, 244, 255, 0.12));
	opacity: 0;
	transition: opacity 0.6s ease;
	z-index: -1;
}

.panel::after {
	content: '';
	position: absolute;
	top: 0;
	left: 6%;
	width: 88%;
	height: 1px;
	background: linear-gradient(90deg, transparent, rgba(255, 63, 213, 0.85), rgba(39, 244, 255, 0.85), transparent);
	opacity: 0.35;
}

.panel:hover::before {
	opacity: 1;
}

.panel__header {
	display: flex;
	flex-direction: column;
	gap: 4px;
	margin-bottom: clamp(16px, 2vw, 20px);
	text-transform: uppercase;
}

.panel__title {
	font-family: 'Orbitron', 'Rajdhani', sans-serif;
	font-size: clamp(1.25rem, 1.8vw, 1.8rem);
	font-weight: 600;
	letter-spacing: 0.16em;
	text-shadow: 0 0 16px rgba(255, 63, 213, 0.6);
}

.panel__subtitle {
	font-size: clamp(0.9rem, 1.2vw, 1rem);
	color: var(--text-primary);
	letter-spacing: 0.1em;
}

.panel__tagline {
	font-size: 0.7rem;
	letter-spacing: 0.32em;
	color: var(--text-muted);
}

.stream-status__meta {
	display: flex;
	flex-wrap: wrap;
	gap: 18px;
	margin-bottom: clamp(18px, 2vw, 24px);
	align-items: center;
}

.meta-block {
	min-width: 140px;
	padding: 12px 16px;
	border-radius: 14px;
	background: rgba(20, 12, 42, 0.65);
	border: 1px solid rgba(255, 63, 213, 0.12);
	box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.02);
}

.meta-label {
	display: block;
	font-size: 0.65rem;
	text-transform: uppercase;
	letter-spacing: 0.24em;
	color: rgba(255, 255, 255, 0.6);
}

.meta-value {
	display: block;
	font-family: 'Share Tech Mono', 'Orbitron', sans-serif;
	font-size: 1.35rem;
	line-height: 1.2;
	color: var(--accent-cyan);
	text-shadow: 0 0 16px rgba(39, 244, 255, 0.5);
}

.meta-tags {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 0;
	margin: 0;
	list-style: none;
}

.meta-tags li {
	padding: 8px 14px;
	border-radius: 999px;
	background: rgba(39, 244, 255, 0.12);
	border: 1px solid rgba(39, 244, 255, 0.32);
	text-transform: uppercase;
	font-size: 0.65rem;
	letter-spacing: 0.28em;
}

.stream-goals {
	display: flex;
	flex-direction: column;
	gap: 12px;
}

.stream-goals ul {
	margin: 0;
	padding: 0;
	display: flex;
	flex-direction: column;
	gap: 12px;
	list-style: none;
}

.stream-goal {
	position: relative;
	display: flex;
	align-items: center;
	gap: 14px;
	padding: 12px 16px;
	border-radius: 16px;
	background: rgba(12, 6, 28, 0.72);
	border: 1px solid rgba(148, 96, 255, 0.32);
	box-shadow: inset 0 0 18px rgba(255, 63, 213, 0.12);
	transition: transform 0.3s ease, box-shadow 0.3s ease;
}

.stream-goal::after {
	content: attr(data-status);
	position: absolute;
	top: 12px;
	right: 16px;
	font-size: 0.55rem;
	letter-spacing: 0.26em;
	text-transform: uppercase;
	color: rgba(255, 255, 255, 0.4);
}

.stream-goal__indicator {
	width: 12px;
	height: 56px;
	border-radius: 8px;
	background: linear-gradient(180deg, rgba(39, 244, 255, 0.6), rgba(255, 63, 213, 0.6));
	box-shadow: 0 0 16px rgba(255, 63, 213, 0.66);
	position: relative;
	overflow: hidden;
}

.stream-goal[data-status='pending'] .stream-goal__indicator {
	filter: saturate(0.6);
	opacity: 0.7;
}

.stream-goal[data-status='complete'] .stream-goal__indicator {
	background: linear-gradient(180deg, rgba(39, 244, 255, 1), rgba(39, 244, 255, 0.8));
}

.stream-goal__body {
	display: flex;
	flex-direction: column;
	gap: 2px;
}

.stream-goal__label {
	font-size: 0.95rem;
	text-transform: uppercase;
	letter-spacing: 0.12em;
}

.stream-goal__detail {
	font-size: 0.8rem;
	color: rgba(222, 219, 255, 0.72);
}

.section-title {
	font-size: 0.78rem;
	letter-spacing: 0.32em;
	text-transform: uppercase;
	color: rgba(255, 255, 255, 0.72);
}

.sprint-panel__summary {
	display: flex;
	gap: 16px;
	margin-bottom: 16px;
}

.summary-card {
	flex: 1 1 0;
	padding: 14px 16px;
	border-radius: 16px;
	background: rgba(13, 7, 30, 0.66);
	border: 1px solid rgba(39, 244, 255, 0.2);
	box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.04);
}

.summary-label {
	display: block;
	font-size: 0.6rem;
	letter-spacing: 0.28em;
	text-transform: uppercase;
	color: rgba(255, 255, 255, 0.56);
}

.summary-value {
	display: block;
	margin-top: 6px;
	font-family: 'Share Tech Mono', 'Rajdhani', sans-serif;
	font-size: 1.4rem;
	color: var(--accent-magenta);
	text-shadow: 0 0 18px rgba(255, 63, 213, 0.66);
}

.sprint-goals {
	margin: 0;
	padding: 0;
	display: flex;
	flex-direction: column;
	gap: 10px;
	list-style: none;
}

.sprint-goal {
	display: grid;
	grid-template-columns: auto 1fr auto;
	align-items: center;
	gap: 12px;
	padding: 12px 14px;
	border-radius: 14px;
	background: rgba(16, 9, 36, 0.72);
	border: 1px solid rgba(138, 94, 255, 0.3);
	transition: transform 0.3s ease, border-color 0.3s ease;
}

.sprint-goal[data-mood='focus'] {
	border-color: rgba(39, 244, 255, 0.36);
	box-shadow: inset 0 0 18px rgba(39, 244, 255, 0.16);
}

.sprint-goal[data-mood='stretch'] {
	border-color: rgba(255, 63, 213, 0.36);
	box-shadow: inset 0 0 18px rgba(255, 63, 213, 0.18);
}

.sprint-goal[data-mood='restore'] {
	border-color: rgba(255, 176, 46, 0.36);
	box-shadow: inset 0 0 18px rgba(255, 176, 46, 0.18);
}

.sprint-goal[data-status='complete'] {
	opacity: 0.6;
}

.sprint-goal__glyph {
	font-size: 1.2rem;
}

.sprint-goal__label {
	font-size: 0.95rem;
	letter-spacing: 0.08em;
	text-transform: uppercase;
}

.sprint-goal__status {
	font-size: 0.7rem;
	color: rgba(222, 220, 255, 0.72);
	letter-spacing: 0.22em;
	text-transform: uppercase;
}

.information-panel {
	gap: 16px;
}

.info-slide {
	flex: 1;
	display: flex;
	flex-direction: column;
	gap: 12px;
	padding: 18px;
	border-radius: 18px;
	background: rgba(9, 6, 26, 0.68);
	border: 1px solid rgba(39, 244, 255, 0.24);
	box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.03), 0 12px 32px rgba(6, 5, 40, 0.6);
}

.info-slide__title {
	font-size: 1.05rem;
	letter-spacing: 0.18em;
	text-transform: uppercase;
	color: var(--accent-cyan);
}

.info-slide__caption {
	font-size: 0.82rem;
	color: rgba(222, 220, 255, 0.7);
}

.info-metrics {
	margin: 0;
	padding: 0;
	list-style: none;
	display: grid;
	gap: 12px;
}

.info-metric {
	display: grid;
	grid-template-columns: 1fr auto;
	grid-template-areas:
		'label value'
		'detail value';
	gap: 6px;
	padding: 12px 14px;
	border-radius: 14px;
	background: rgba(13, 9, 32, 0.75);
	border: 1px solid rgba(146, 96, 255, 0.28);
}

.info-metric[data-tone='positive'] {
	border-color: rgba(39, 244, 255, 0.4);
}

.info-metric[data-tone='warning'] {
	border-color: rgba(255, 176, 46, 0.4);
}

.info-metric__label {
	grid-area: label;
	font-size: 0.75rem;
	text-transform: uppercase;
	letter-spacing: 0.24em;
	color: rgba(232, 229, 255, 0.58);
}

.info-metric__value {
	grid-area: value;
	font-family: 'Share Tech Mono', 'Rajdhani', sans-serif;
	font-size: 1.2rem;
	text-align: right;
	color: var(--accent-magenta);
	text-shadow: 0 0 18px rgba(255, 63, 213, 0.5);
}

.info-metric[data-tone='positive'] .info-metric__value {
	color: var(--accent-cyan);
	text-shadow: 0 0 18px rgba(39, 244, 255, 0.5);
}

.info-metric[data-tone='warning'] .info-metric__value {
	color: var(--accent-amber);
	text-shadow: 0 0 18px rgba(255, 176, 46, 0.5);
}

.info-metric__detail {
	grid-area: detail;
	font-size: 0.75rem;
	color: rgba(210, 206, 255, 0.6);
}

.info-dots {
	display: flex;
	justify-content: center;
	gap: 12px;
	margin-top: 18px;
}

.info-dot {
	position: relative;
	width: 14px;
	height: 14px;
	border-radius: 50%;
	border: 1px solid rgba(255, 255, 255, 0.2);
	background: rgba(255, 255, 255, 0.08);
	cursor: pointer;
	transition: transform 0.3s ease, border-color 0.3s ease;
}

.info-dot[data-active='true'] {
	border-color: var(--accent-cyan);
	transform: scale(1.1);
}

.info-dot__glow {
	position: absolute;
	inset: -6px;
	border-radius: 50%;
	background: radial-gradient(circle, rgba(39, 244, 255, 0.5), transparent 65%);
	opacity: 0;
	transition: opacity 0.3s ease;
}

.info-dot[data-active='true'] .info-dot__glow {
	opacity: 1;
}

.panel-transition-enter-active,
.panel-transition-leave-active {
	transition: opacity 0.45s ease, transform 0.45s ease;
}

.panel-transition-enter-from,
.panel-transition-leave-to {
	opacity: 0;
	transform: translateY(10px) scale(0.98);
}

.chat-feed {
	margin: 0;
	padding: 0;
	list-style: none;
	display: flex;
	flex-direction: column;
	gap: 12px;
}

.chat-line {
	display: grid;
	grid-template-columns: auto auto 1fr;
	align-items: center;
	gap: 12px;
	padding: 10px 14px;
	border-radius: 14px;
	background: rgba(13, 6, 28, 0.7);
	border: 1px solid rgba(255, 63, 213, 0.22);
}

.chat-line__timestamp {
	font-family: 'Share Tech Mono', monospace;
	font-size: 0.7rem;
	color: rgba(255, 255, 255, 0.45);
}

.chat-line__author {
	font-weight: 600;
	text-transform: uppercase;
	letter-spacing: 0.14em;
	display: flex;
	align-items: center;
	gap: 8px;
}

.chat-line__badge {
	font-size: 0.55rem;
	padding: 2px 6px;
	border-radius: 6px;
	background: rgba(255, 255, 255, 0.12);
	letter-spacing: 0.2em;
}

.chat-line__badge[data-role='mod'] {
	background: rgba(39, 244, 255, 0.22);
	border: 1px solid rgba(39, 244, 255, 0.4);
}

.chat-line__badge[data-role='vip'] {
	background: rgba(255, 63, 213, 0.22);
	border: 1px solid rgba(255, 63, 213, 0.4);
}

.chat-line__badge[data-role='broadcaster'] {
	background: rgba(255, 176, 46, 0.22);
	border: 1px solid rgba(255, 176, 46, 0.4);
}

.chat-line__message {
	font-size: 0.9rem;
	color: rgba(231, 227, 255, 0.84);
}

.alerts-feed {
	margin: 0;
	padding: 0;
	list-style: none;
	display: flex;
	flex-direction: column;
	gap: 12px;
}

.alert-line {
	display: grid;
	grid-template-columns: auto 1fr auto;
	gap: 12px;
	padding: 12px 16px;
	border-radius: 16px;
	background: rgba(17, 8, 34, 0.72);
	border: 1px solid rgba(148, 96, 255, 0.28);
	box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.02);
}

.alert-line__icon {
	font-size: 1.4rem;
	filter: drop-shadow(0 0 12px currentColor);
}

.alert-line__headline {
	display: flex;
	justify-content: space-between;
	align-items: center;
	font-size: 0.85rem;
	letter-spacing: 0.1em;
	text-transform: uppercase;
}

.alert-line__user {
	font-weight: 600;
}

.alert-line__type {
	font-size: 0.6rem;
	letter-spacing: 0.32em;
	opacity: 0.7;
}

.alert-line__detail {
	margin: 0;
	font-size: 0.9rem;
	color: rgba(222, 219, 255, 0.78);
}

.alert-line__time {
	font-size: 0.7rem;
	color: rgba(255, 255, 255, 0.4);
	text-transform: uppercase;
	letter-spacing: 0.22em;
}

.sprint-timer {
	display: flex;
	flex-direction: column;
	align-items: center;
	text-align: center;
}

.timer-display {
	position: relative;
	width: min(360px, 100%);
	aspect-ratio: 1 / 1;
	display: flex;
	align-items: center;
	justify-content: center;
}

.timer-ring {
	position: relative;
	width: 100%;
	height: 100%;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	background: radial-gradient(circle at 50% 52%, rgba(6, 6, 18, 0.9), rgba(8, 4, 20, 0.6));
	box-shadow: 0 0 45px rgba(39, 244, 255, 0.25);
}

.timer-ring::before {
	content: '';
	position: absolute;
	inset: 6px;
	border-radius: 50%;
	background: rgba(7, 4, 18, 0.92);
	border: 1px solid rgba(148, 96, 255, 0.28);
	z-index: 0;
}

.timer-ring::after {
	content: '';
	position: absolute;
	inset: 0;
	border-radius: 50%;
	padding: 6px;
	background: conic-gradient(from -90deg, var(--accent-cyan) var(--progress-angle), rgba(255, 255, 255, 0.06) var(--progress-angle));
	-webkit-mask: radial-gradient(circle, transparent calc(50% - 18px), black calc(50% - 17px));
	mask: radial-gradient(circle, transparent calc(50% - 18px), black calc(50% - 17px));
	filter: drop-shadow(0 0 24px rgba(39, 244, 255, 0.5));
}

.timer-core {
	position: relative;
	z-index: 1;
	width: 72%;
	aspect-ratio: 1 / 1;
	border-radius: 50%;
	background: radial-gradient(circle at 50% 30%, rgba(39, 244, 255, 0.25), rgba(7, 4, 18, 0.9));
	border: 1px solid rgba(255, 63, 213, 0.22);
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
	gap: 8px;
	box-shadow: inset 0 0 22px rgba(255, 63, 213, 0.2);
}

.timer-core__value {
	font-family: 'Share Tech Mono', 'Orbitron', sans-serif;
	font-size: clamp(1.8rem, 6vw, 2.6rem);
	letter-spacing: 0.24em;
	text-transform: uppercase;
	color: var(--accent-cyan);
	text-shadow: 0 0 24px rgba(39, 244, 255, 0.8);
}

.timer-core__label {
	font-size: 0.68rem;
	letter-spacing: 0.32em;
	text-transform: uppercase;
	color: rgba(222, 219, 255, 0.62);
}

.timer-footnote {
	margin-top: 18px;
	font-size: 0.75rem;
	letter-spacing: 0.22em;
	text-transform: uppercase;
	color: rgba(222, 220, 255, 0.6);
}

@media (max-width: 1024px) {
	.overlay-grid {
		grid-template-columns: 1fr;
		grid-template-areas:
			'status'
			'sprint'
			'info'
			'chat'
			'alerts'
			'timer';
	}

	.information-panel {
		min-height: auto;
	}

	.sprint-panel__summary {
		flex-direction: column;
	}

	.timer-display {
		width: min(320px, 100%);
	}
}

@media (max-width: 640px) {
	.overlay {
		padding: clamp(16px, 7vw, 32px);
	}

	.panel {
		padding: clamp(16px, 4vw, 24px);
	}

	.stream-status__meta {
		flex-direction: column;
		align-items: stretch;
	}

	.meta-tags {
		flex-wrap: wrap;
		justify-content: center;
	}

	.chat-line {
		grid-template-columns: auto 1fr;
		grid-template-rows: auto auto;
	}

	.chat-line__message {
		grid-column: 1 / -1;
	}

	.alert-line {
		grid-template-columns: auto 1fr;
	}

	.alert-line__time {
		justify-self: start;
	}
}

@keyframes driftOne {
	0% {
		transform: translate3d(0, 0, 0) scale(1);
	}
	100% {
		transform: translate3d(8%, -6%, 0) scale(1.08);
	}
}

@keyframes driftTwo {
	0% {
		transform: translate3d(0, 0, 0) scale(1);
	}
	100% {
		transform: translate3d(-6%, 8%, 0) scale(1.12);
	}
}

@keyframes scanlines {
	0% {
		transform: translateY(0);
	}
	100% {
		transform: translateY(2px);
	}
}
</style>
