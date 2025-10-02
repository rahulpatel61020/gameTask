using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CardGridLayout : LayoutGroup
{
    public int rows;
    public int colums;

    public Vector2 cardSize;

    public Vector2 spacing;
    public int preferredTopPadding;
    public override void SetLayoutHorizontal()

    {
        return;
    }
    public override void SetLayoutVertical()

    {
        return;
    }

    // Update is called once per frame
    void Update()
    {

    }
    public override void CalculateLayoutInputVertical()
    {
        if(rows==0||colums==0)
        {
            rows = 4;
            colums = 5;
        }
        float parentWidth = rectTransform.rect.width;
        float parentHeight = rectTransform.rect.height;

        float cardHeight = (parentHeight - 2 * preferredTopPadding-spacing.y*(rows-1)) / rows;
        float cardWidth = cardHeight;
      
        if (cardWidth * colums + spacing.x * (colums - 1) > parentWidth)
        {
            cardWidth = (parentWidth - 2 * preferredTopPadding - (colums - 1) * spacing.x) / colums;
            cardHeight = cardWidth;
        }
                
        cardSize = new Vector2(cardWidth, cardHeight);
        padding.left = Mathf.FloorToInt((parentWidth-colums*cardWidth-spacing.x * (colums-1 ))/2);
        padding.top = Mathf.FloorToInt((parentHeight - rows * cardHeight - spacing.y * (rows - 1)) / 2);
        padding.bottom = padding.top;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            int rowCount = i / colums;
            int coloumCount=i%colums;
            var item = rectChildren[i];
            var xPos = padding.left+ cardSize.x * coloumCount+spacing.x*(coloumCount);

            var yPos = cardSize.y*rowCount+spacing.y*(rowCount);
            SetChildAlongAxis(item,0,xPos,cardSize.x);
            SetChildAlongAxis(item, 1, yPos, cardSize.y);

        }
    }
}

