using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    internal sealed class CardBoardEntityRegistry
    {
        internal sealed class Slot
        {
            public CardModel Model;
            public CardEntity Entity;
        }

        private readonly Dictionary<CardInstanceId, Slot> slots = new Dictionary<CardInstanceId, Slot>();

        public int Count => slots.Count;

        public void Clear()
        {
            slots.Clear();
        }

        public IEnumerable<KeyValuePair<CardInstanceId, Slot>> Entries => slots;

        public void Register(CardModel model, CardEntity entity)
        {
            if (model == null || !model.InstanceId.IsValid || entity == null)
            {
                return;
            }

            slots[model.InstanceId] = new Slot
            {
                Model = model,
                Entity = entity,
            };
        }

        public bool TryGetView(CardInstanceId id, out ICardBattleView view)
        {
            view = null;
            if (!id.IsValid)
            {
                return false;
            }

            if (slots.TryGetValue(id, out var slot) && slot.Entity != null)
            {
                view = slot.Entity;
                return true;
            }

            return false;
        }

        public bool TryGetModel(CardInstanceId id, out CardModel model)
        {
            model = null;
            if (!id.IsValid)
            {
                return false;
            }

            if (slots.TryGetValue(id, out var slot) && slot.Model != null)
            {
                model = slot.Model;
                return true;
            }

            return false;
        }

        public bool TryGetEntity(CardModel model, out CardEntity entity)
        {
            entity = null;
            if (model == null || !model.InstanceId.IsValid)
            {
                return false;
            }

            if (!slots.TryGetValue(model.InstanceId, out var slot))
            {
                return false;
            }

            entity = slot.Entity;
            return entity != null;
        }

        public bool HasEntity(CardModel model)
        {
            return model != null
                && model.InstanceId.IsValid
                && slots.ContainsKey(model.InstanceId);
        }
    }
}
